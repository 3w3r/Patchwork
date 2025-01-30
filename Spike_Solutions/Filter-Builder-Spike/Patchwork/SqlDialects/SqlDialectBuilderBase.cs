using System.Data.Common;
using Azure;
using Patchwork.DbSchema;
using Patchwork.Expansion;
using Patchwork.Fields;
using Patchwork.Filters;
using Patchwork.Paging;
using Patchwork.Sort;
using Patchwork.SqlStatements;

namespace Patchwork.SqlDialects
{
  public abstract class SqlDialectBuilderBase : ISqlDialectBuilder
  {

    protected readonly string _connectionString;
    protected static Dictionary<string, DatabaseMetadata> _metadataCache = new Dictionary<string, DatabaseMetadata>();
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

    protected abstract DbConnection GetConnection();
    public virtual DatabaseMetadata DiscoverSchema()
    {
      if (_metadata != null || _metadataCache.TryGetValue(_connectionString, out _metadata))
      {
        return _metadata;
      }

      SchemaDiscoveryBuilder schemaDiscoveryBuilder = new SchemaDiscoveryBuilder();
      using DbConnection connection = GetConnection();
      _metadata = schemaDiscoveryBuilder.ReadSchema(connection);
      _metadataCache[_connectionString] = _metadata;
      return _metadata;
    }

    public virtual SelectStatement BuildGetListSql(string schemaName, string entityName, string fields = "", string filter = "", string sort = "", int limit = 0, int offset = 0)
    {
      if (string.IsNullOrEmpty(schemaName))
        throw new ArgumentException("Schema name is required.", nameof(schemaName));
      if (string.IsNullOrEmpty(entityName))
        throw new ArgumentException("Entity name is required.", nameof(entityName));

      var select = BuildSelectClause(fields, entityName);
      var where = string.IsNullOrEmpty(filter) ? null : BuildWhereClause(filter, entityName);
      var orderBy = string.IsNullOrEmpty(sort) ? "" : BuildOrderByClause(sort, entityName);
      var paging = BuildLimitOffsetClause(limit, offset);

      return new SelectStatement($"{select} {where?.Sql} {orderBy} {paging}", where?.Parameters ?? new Dictionary<string, object>());
    }
    public virtual SelectStatement BuildGetSingleSql(string schemaName, string entityName, string id, string fields = "", string include = "", DateTimeOffset? asOf = null)
    {
      if (string.IsNullOrEmpty(schemaName))
        throw new ArgumentException("Schema name is required.", nameof(schemaName));
      if (string.IsNullOrEmpty(entityName))
        throw new ArgumentException("Entity name is required.", nameof(entityName));

      Entity entity = FindEntity(entityName);
      string select = BuildSelectClause(fields, entityName);
      string join = BuildJoinClause(include, entityName);
      string where = BuildGetByPkClause(entityName);
      var parameters = new Dictionary<string, object> { { "id", id } };

      return new SelectStatement($"{select} {join} {where}", parameters);
    }
    
    public abstract string BuildPatchListSql(string schemaName, string entityName, JsonPatchDocument jsonPatchRequestBody);
    public abstract string BuildPutSingleSql(string schemaName, string entityName, string id, string jsonResourceRequestBody);
    public abstract string BuildPatchSingleSql(string schemaName, string entityName, string id, JsonPatchDocument jsonPatchRequestBody);
    public abstract string BuildDeleteSingleSql(string schemaName, string entityName, string id);

    public abstract string BuildSelectClause(string fields, string entityName);
    public abstract string BuildJoinClause(string includeString, string entityName);
    public abstract FilterStatement BuildWhereClause(string filterString, string entityName);
    public abstract string BuildGetByPkClause(string entityName);
    public abstract string BuildOrderByClause(string sort, string entityName);
    public abstract string BuildLimitOffsetClause(int limit, int offset);

    protected Entity FindEntity(string entityName)
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
  }
}
