using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Json.Patch;
using Patchwork.DbSchema;
using Patchwork.Expansion;
using Patchwork.Extensions;
using Patchwork.Fields;
using Patchwork.Filters;
using Patchwork.Paging;
using Patchwork.Sort;
using Patchwork.SqlStatements;

namespace Patchwork.SqlDialects;
public abstract class SqlDialectBuilderBase : ISqlDialectBuilder
{
  protected readonly string _connectionString;
  protected static ConcurrentDictionary<string, DatabaseMetadata> _metadataCache = new ConcurrentDictionary<string, DatabaseMetadata>();
  protected DatabaseMetadata? _metadata = null;
  public string DefaultSchemaName { get; init; }

  public SqlDialectBuilderBase(string connectionString, string defaultSchema)
  {
    _connectionString = connectionString;
    DefaultSchemaName = defaultSchema;
  }
  public SqlDialectBuilderBase(DatabaseMetadata metadata, string defaultSchema)
  {
    _connectionString = string.Empty;
    _metadata = metadata;
    DefaultSchemaName = defaultSchema;
  }

  public abstract WriterConnection GetWriterConnection();
  public abstract ReaderConnection GetReaderConnection();
  public Entity FindEntity(string schemaName, string entityName)
  {
    if (string.IsNullOrEmpty(entityName))
      throw new ArgumentException(nameof(entityName));
    DiscoverSchema();
    if (_metadata == null)
      throw new ArgumentException("Cannot access database schema");

    Entity? entity = _metadata.Schemas
                              .Where(s => s.Name.Equals(schemaName, StringComparison.OrdinalIgnoreCase))
                              .SelectMany(x => x.Tables)
                              .FirstOrDefault(t => t.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));
    if (entity == null)
      entity = _metadata.Schemas
                        .Where(s => s.Name.Equals(schemaName, StringComparison.OrdinalIgnoreCase))
                        .SelectMany(x => x.Views)
                        .FirstOrDefault(t => t.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));
    if (entity == null)
      throw new ArgumentException($"Invalid Table or View Name: {entityName}");
    return entity;
  }
  public virtual string GetPkValue(string schemaName, string entityName, object entityObject)
  {
    Entity entity = FindEntity(schemaName, entityName);
    if (entity.PrimaryKey == null)
      return string.Empty;

    // If the entityObject was queried from Dapper, then it can be case as an IDictionary
    IDictionary<string, object?>? row = entityObject as IDictionary<string, object?>;
    if (row != null && row.ContainsKey(entity.PrimaryKey.Name))
    {
      return row[entity.PrimaryKey.Name]?.ToString() ?? string.Empty;
    }
    else
    {
      // If it was not from Dapper, then we have to use Reflection to get the value.
      Type type = entityObject.GetType();
      PropertyInfo? property = type.GetProperty(entity.PrimaryKey.Name);
      if (property == null)
        return string.Empty;

      object? prop = property.GetValue(entityObject);
      return prop?.ToString() ?? string.Empty;
    }
  }
  public virtual DatabaseMetadata DiscoverSchema()
  {
    if (_metadata != null || _metadataCache.TryGetValue(_connectionString, out _metadata))
    {
      return _metadata;
    }

    SchemaDiscoveryBuilder schemaDiscoveryBuilder = new SchemaDiscoveryBuilder();
    using WriterConnection connection = GetWriterConnection();
    _metadata = schemaDiscoveryBuilder.ReadSchema(connection);
    _metadataCache.TryAdd(_connectionString, _metadata);
    return _metadata;
  }
  public virtual bool HasPatchTrackingEnabled()
  {
    if (_metadata == null)
      this.DiscoverSchema();
    return _metadata!.HasPatchTracking;
  }

  public virtual SelectListStatement BuildGetListSql(string schemaName, string entityName, string fields = "", string filter = "", string sort = "", int limit = 0, int offset = 0)
  {
    if (string.IsNullOrEmpty(schemaName))
      throw new ArgumentException("Schema name is required.", nameof(schemaName));
    if (string.IsNullOrEmpty(entityName))
      throw new ArgumentException("Entity name is required.", nameof(entityName));

    Entity entity = FindEntity(schemaName, entityName);
    string select = BuildSelectClause(fields, entity);
    string count = BuildCountClause(entity);
    FilterStatement? where = string.IsNullOrEmpty(filter) ? null : BuildWhereClause(filter, entity);
    string orderBy = BuildOrderByClause(sort, entity);
    string paging = BuildLimitOffsetClause(limit, offset);
    where?.Parameters.SetParameterDataTypes(entity);

    return new SelectListStatement(
      $"{select} {where?.Sql} {orderBy} {paging}",
      $"{count} {where?.Sql}",
      where?.Parameters ?? new Dictionary<string, object>());
  }
  public virtual SelectResourceStatement BuildGetSingleSql(string schemaName, string entityName, string id, string fields = "", string include = "", DateTimeOffset? asOf = null)
  {
    if (string.IsNullOrEmpty(schemaName))
      throw new ArgumentException("Schema name is required.", nameof(schemaName));
    if (string.IsNullOrEmpty(entityName))
      throw new ArgumentException("Entity name is required.", nameof(entityName));

    Entity entity = FindEntity(schemaName, entityName);
    string select = BuildSelectClause(fields, entity);
    string join = string.IsNullOrEmpty(include) ? "" : BuildJoinClause(include, entity);
    string where = BuildWherePkForGetClause(entity);
    Dictionary<string, object> parameters = new Dictionary<string, object> { { "id", id } };
    parameters.SetParameterDataTypes(entity);

    return new SelectResourceStatement($"{select} {join} {where}", parameters);
  }

  public virtual InsertStatement BuildPostSingleSql(string schemaName, string entityName, JsonDocument jsonResourceRequestBody)
  {
    if (string.IsNullOrEmpty(schemaName))
      throw new ArgumentException("Schema name is required.", nameof(schemaName));
    if (string.IsNullOrEmpty(entityName))
      throw new ArgumentException("Entity name is required.", nameof(entityName));
    if (jsonResourceRequestBody == null)
      throw new ArgumentException("JsonResourceRequestBody name is required.", nameof(jsonResourceRequestBody));

    Entity entity = FindEntity(schemaName, entityName);
    string insert = BuildInsertClause(entity);
    string columnList = BuildColumnListForInsert(entity);
    string paramsList = BuildParameterListForInsert(entity);

    Dictionary<string, object> parameters = new Dictionary<string, object>();
    parameters.AddJsonResourceToDictionary(jsonResourceRequestBody);
    parameters.SetParameterDataTypes(entity);

    return new InsertStatement($"{insert} {columnList} {paramsList}", parameters);
  }

  public virtual UpdateStatement BuildPutSingleSql(string schemaName, string entityName, string id, JsonDocument jsonResourceRequestBody)
  {
    if (string.IsNullOrEmpty(schemaName))
      throw new ArgumentException("Schema name is required.", nameof(schemaName));
    if (string.IsNullOrEmpty(entityName))
      throw new ArgumentException("Entity name is required.", nameof(entityName));
    if (string.IsNullOrEmpty(id))
      throw new ArgumentException("Entity Id name is required.", nameof(id));
    if (jsonResourceRequestBody == null)
      throw new ArgumentException("JsonResourceRequestBody name is required.", nameof(jsonResourceRequestBody));

    Entity entity = FindEntity(schemaName, entityName);
    string update = BuildUpdateClause(entity);
    string where = BuildWherePkForUpdateClause(entity);

    Dictionary<string, object> parameters = new Dictionary<string, object>() { { "id", id } };
    parameters.AddJsonResourceToDictionary(jsonResourceRequestBody);
    parameters.SetParameterDataTypes(entity);
    string sets = BuildSetClause(parameters, entity);

    return new UpdateStatement($"{update} {sets} {where}", parameters);
  }
  public virtual DeleteStatement BuildDeleteSingleSql(string schemaName, string entityName, string id)
  {
    if (string.IsNullOrEmpty(schemaName))
      throw new ArgumentException("Schema name is required.", nameof(schemaName));
    if (string.IsNullOrEmpty(entityName))
      throw new ArgumentException("Entity name is required.", nameof(entityName));
    if (string.IsNullOrEmpty(id))
      throw new ArgumentException("Entity Id name is required.", nameof(id));

    Entity entity = FindEntity(schemaName, entityName);
    string delete = BuildDeleteClause(entity);
    string where = BuildWherePkForDeleteClause(entity);
    Dictionary<string, object> parameters = new Dictionary<string, object>() { { "id", id } };
    parameters.SetParameterDataTypes(entity);

    return new DeleteStatement($"{delete} {where}", parameters);
  }

  public virtual PatchStatement BuildPatchListSql(string schemaName, string entityName, JsonPatch jsonPatchRequestBody) { throw new NotImplementedException(); }
  public virtual PatchStatement BuildPatchSingleSql(string schemaName, string entityName, string id, JsonPatch jsonPatchRequestBody) { throw new NotImplementedException(); }

  public virtual JsonPatch BuildDiffAsJsonPatch(string original, string modified)
  {
    return BuildDiffAsJsonPatch(JsonDocument.Parse(original), JsonDocument.Parse(modified));
  }
  public virtual JsonPatch BuildDiffAsJsonPatch(JsonDocument original, JsonDocument modified)
  {
    JsonPatch patch = original.CreatePatch(modified);
    return patch;
  }

  public virtual InsertStatement GetInsertStatementForPatchworkLog(string schemaName, string entityName, string id, JsonPatch jsonPatchRequestBody)
  {
    Dictionary<string, object> insertParams = new Dictionary<string, object>();
    insertParams.Add("schemaname", schemaName);
    insertParams.Add("entityname", entityName);
    insertParams.Add("id", id);
    insertParams.Add("patch", JsonSerializer.Serialize(jsonPatchRequestBody));
    return new InsertStatement(this.GetInsertPatchTemplate(), insertParams);
  }
  protected abstract string GetInsertPatchTemplate();

  internal abstract string BuildSelectClause(string fields, Entity entity);
  internal abstract string BuildCountClause(Entity entity);
  internal abstract string BuildJoinClause(string includeString, Entity entity);
  internal abstract FilterStatement BuildWhereClause(string filterString, Entity entity);
  internal abstract string BuildWherePkForGetClause(Entity entity);
  internal abstract string BuildOrderByClause(string sort, Entity entity);
  internal abstract string BuildLimitOffsetClause(int limit, int offset);

  internal abstract string BuildInsertClause(Entity entity);
  internal abstract string BuildColumnListForInsert(Entity entity);
  internal abstract string BuildParameterListForInsert(Entity entity);

  internal abstract string BuildUpdateClause(Entity entity);
  internal abstract string BuildSetClause(Dictionary<string, object> parameters, Entity entity);
  internal abstract string BuildWherePkForUpdateClause(Entity entity);

  internal abstract string BuildDeleteClause(Entity entity);
  internal abstract string BuildWherePkForDeleteClause(Entity entity);

  protected List<FieldsToken> GetFieldTokens(string fields, Entity entity)
  {
    FieldsLexer lexer = new FieldsLexer(fields, entity, DiscoverSchema());
    List<FieldsToken> tokens = lexer.Tokenize();
    return tokens;
  }
  protected List<FilterToken> GetFilterTokens(string filterString, Entity entity)
  {
    if (string.IsNullOrWhiteSpace(filterString))
      throw new ArgumentException("No input string");
    if (entity == null)
      throw new ArgumentNullException(nameof(entity));

    FilterLexer lexer = new FilterLexer(filterString, entity, DiscoverSchema());
    List<FilterToken> tokens = lexer.Tokenize();

    if (tokens.Count == 0)
      throw new ArgumentException("No valid tokens found");

    foreach (FilterToken token in tokens)
    {
      if (
           token.Type == FilterTokenType.Identifier &&
           !entity.Columns.Any(c => c.Name.Equals(token.Value, StringComparison.OrdinalIgnoreCase))
         )
      {
        throw new ArgumentException($@"{token.Value} is not a column on {entity.Name}", nameof(filterString));
      }
    }
    return tokens;
  }
  protected List<IncludeToken> GetIncludeTokens(string includeString, Entity entity)
  {
    if (string.IsNullOrEmpty(includeString))
      throw new ArgumentException(nameof(includeString));
    if (entity == null)
      throw new ArgumentNullException(nameof(entity));

    IncludeLexer lexer = new IncludeLexer(includeString, entity, DiscoverSchema());
    List<IncludeToken> tokens = lexer.Tokenize();
    return tokens;
  }
  protected List<SortToken> GetSortTokens(string sort, Entity entity)
  {
    if (entity == null)
      throw new ArgumentNullException(nameof(entity));

    SortLexer lexer = new SortLexer(sort, entity, DiscoverSchema());
    List<SortToken> tokens = lexer.Tokenize();

    return tokens;
  }
  protected PagingToken GetPagingToken(int limit, int offset)
  {
    return new PagingToken(limit, offset);
  }

}
