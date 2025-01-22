using Patchwork.DbSchema;
using Patchwork.Expansion;
using Patchwork.Filters;
using Patchwork.Sort;
using System.Data.Common;

namespace Patchwork.SqlDialects
{
  public abstract class SqlDialectBuilderBase : ISqlDialectBuilder
  {

    protected readonly string _connectionString;
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
      if (_metadata != null) return _metadata;

      var schemaDiscoveryBuilder = new SchemaDiscoveryBuilder();
      using (var connection = GetConnection())
      {
        return schemaDiscoveryBuilder.ReadSchema(connection);
      }
    }

    public abstract string BuildSelectClause(string entityName);
    public abstract string BuildJoinClause(string includeString, string entityName);
    public abstract string BuildWhereClause(string filterString, string entityName);
    public abstract string BuildOrderByClause(string sort, string pkName, string entityName);
    public abstract string BuildLimitOffsetClause(int limit, int offset);

    protected Entity FindEntity(string entityName)
    {
      if (string.IsNullOrEmpty(entityName)) throw new ArgumentException(nameof(entityName));
      DiscoverSchema();
      if (_metadata == null) throw new ArgumentException("Cannot access database schema");
      var entity = _metadata.Schemas
                            .SelectMany(x => x.Tables)
                            .FirstOrDefault(t => t.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));
      if (entity == null)
        entity = _metadata.Schemas
                            .SelectMany(x => x.Views)
                            .FirstOrDefault(t => t.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));
      if (entity == null) throw new ArgumentException($"Invalid Table or View Name: {entityName}");
      return entity;
    }

    protected List<FilterToken> GetFilterTokens(string filterString, Entity entity)
    {
      if (string.IsNullOrWhiteSpace(filterString))
        throw new ArgumentException("No input string");
      if (entity == null) throw new ArgumentNullException(nameof(entity));

      var lexer = new FilterLexer(filterString);
      var tokens = lexer.Tokenize();

      if (tokens.Count == 0)
        throw new ArgumentException("No valid tokens found");

      foreach (var token in tokens)
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

      var lexer = new IncludeLexer(includeString, entity, DiscoverSchema());
      var tokens = lexer.Tokenize();
      return tokens;
    }

    protected List<SortToken> GetSortTokens(string sort, Entity entity)
    {
      if (entity == null)
        throw new ArgumentNullException(nameof(entity));

      var lexer = new SortLexer(sort);
      var tokens = lexer.Tokenize();

      if (entity.PrimaryKey != null)
        tokens.Add(new SortToken(entity.PrimaryKey.Name, SortDirection.Ascending));

      return tokens;
    }
  }
}
