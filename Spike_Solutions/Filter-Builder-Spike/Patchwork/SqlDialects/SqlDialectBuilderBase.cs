using System.Collections.Concurrent;
using System.Data.Common;
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

  public SqlDialectBuilderBase(string connectionString)
  {
    _connectionString = connectionString;
  }
  public SqlDialectBuilderBase(DatabaseMetadata metadata)
  {
    _connectionString = string.Empty;
    _metadata = metadata;
  }

  public abstract DbConnection GetConnection();
  public virtual DatabaseMetadata DiscoverSchema()
  {
    if (_metadata != null || _metadataCache.TryGetValue(_connectionString, out _metadata))
    {
      return _metadata;
    }

    SchemaDiscoveryBuilder schemaDiscoveryBuilder = new SchemaDiscoveryBuilder();
    using DbConnection connection = GetConnection();
    _metadata = schemaDiscoveryBuilder.ReadSchema(connection);
    _metadataCache.TryAdd(_connectionString, _metadata);
    return _metadata;
  }

  public virtual SelectStatement BuildGetListSql(string schemaName, string entityName, string fields = "", string filter = "", string sort = "", int limit = 0, int offset = 0)
  {
    if (string.IsNullOrEmpty(schemaName))
      throw new ArgumentException("Schema name is required.", nameof(schemaName));
    if (string.IsNullOrEmpty(entityName))
      throw new ArgumentException("Entity name is required.", nameof(entityName));

    var entity = FindEntity(entityName);
    var select = BuildSelectClause(fields, entity);
    var where = string.IsNullOrEmpty(filter) ? null : BuildWhereClause(filter, entity);
    var orderBy = string.IsNullOrEmpty(sort) ? "" : BuildOrderByClause(sort, entity);
    var paging = BuildLimitOffsetClause(limit, offset);

    return new SelectStatement($"{select} {where?.Sql} {orderBy} {paging}", where?.Parameters ?? new Dictionary<string, object>());
  }
  public virtual SelectStatement BuildGetSingleSql(string schemaName, string entityName, string id, string fields = "", string include = "", DateTimeOffset? asOf = null)
  {
    if (string.IsNullOrEmpty(schemaName))
      throw new ArgumentException("Schema name is required.", nameof(schemaName));
    if (string.IsNullOrEmpty(entityName))
      throw new ArgumentException("Entity name is required.", nameof(entityName));

    var entity = FindEntity(entityName);
    string select = BuildSelectClause(fields, entity);
    string join = BuildJoinClause(include, entity);
    string where = BuildWherePkForGetClause(entity);
    var parameters = new Dictionary<string, object> { { "id", id } };

    return new SelectStatement($"{select} {join} {where}", parameters);
  }

  public virtual UpdateStatement BuildPutSingleSql(string schemaName, string entityName, string id, string jsonResourceRequestBody)
  {
    if (string.IsNullOrEmpty(schemaName))
      throw new ArgumentException("Schema name is required.", nameof(schemaName));
    if (string.IsNullOrEmpty(entityName))
      throw new ArgumentException("Entity name is required.", nameof(entityName));
    if (string.IsNullOrEmpty(id))
      throw new ArgumentException("Entity Id name is required.", nameof(id));
    if (string.IsNullOrEmpty(jsonResourceRequestBody))
      throw new ArgumentException("JsonResourceRequestBody name is required.", nameof(jsonResourceRequestBody));

    Entity entity = FindEntity(entityName);
    string update = BuildUpdateClause(entity);
    string where = BuildWherePkForUpdateClause(entity);

    var parameters = new Dictionary<string, object>() { { "id", id } };
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

    Entity entity = FindEntity(entityName);
    string delete = BuildDeleteClause(entity);
    string where = BuildWherePkForDeleteClause(entity);
    var parameters = new Dictionary<string, object>() { { "id", id } };
    parameters.SetParameterDataTypes(entity);

    return new DeleteStatement($"{delete} {where}", parameters);
  }

  public virtual PatchStatement BuildPatchListSql(string schemaName, string entityName, JsonPatch jsonPatchRequestBody) { throw new NotImplementedException(); }
  public virtual PatchStatement BuildPatchSingleSql(string schemaName, string entityName, string id, JsonPatch jsonPatchRequestBody) { throw new NotImplementedException(); }

  internal abstract string BuildSelectClause(string fields, Entity entity);
  internal abstract string BuildJoinClause(string includeString, Entity entity);
  internal abstract FilterStatement BuildWhereClause(string filterString, Entity entity);
  internal abstract string BuildWherePkForGetClause(Entity entity);
  internal abstract string BuildOrderByClause(string sort, Entity entity);
  internal abstract string BuildLimitOffsetClause(int limit, int offset);

  internal abstract string BuildUpdateClause(Entity entity);
  internal abstract string BuildSetClause(Dictionary<string, object> parameters, Entity entity);
  internal abstract string BuildWherePkForUpdateClause(Entity entity);

  internal abstract string BuildDeleteClause(Entity entity);
  internal abstract string BuildWherePkForDeleteClause(Entity entity);

  internal Entity FindEntity(string entityName)
  {
    if (string.IsNullOrEmpty(entityName))
      throw new ArgumentException(nameof(entityName));
    DiscoverSchema();
    if (_metadata == null)
      throw new ArgumentException("Cannot access database schema");
    Entity? entity = _metadata.Schemas
                          .SelectMany(x => x.Tables)
                          .FirstOrDefault(t => t.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));
    if (entity == null)
      entity = _metadata.Schemas
                          .SelectMany(x => x.Views)
                          .FirstOrDefault(t => t.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));
    if (entity == null)
      throw new ArgumentException($"Invalid Table or View Name: {entityName}");
    return entity;
  }
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

  protected object CastParameterValue(Type dataFormat, object value)
  {
    return Convert.ChangeType(value, dataFormat);
  }
}
