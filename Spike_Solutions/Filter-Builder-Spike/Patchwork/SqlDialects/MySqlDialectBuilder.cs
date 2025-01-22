using Patchwork.Filters;
using Patchwork.Sort;
using Patchwork.Paging;
using Patchwork.DbSchema;
using Patchwork.Expansion;
using MySqlConnector;
using System.Data.Common;

namespace Patchwork.SqlDialects
{
  public class MySqlDialectBuilder : SqlDialectBuilderBase
  {
    public MySqlDialectBuilder(string connectionString) : base(connectionString) { }
    public MySqlDialectBuilder(DatabaseMetadata metadata) : base(metadata) { }

    protected override DbConnection GetConnection()
    {
      return new MySqlConnection(_connectionString);
    }

    public override string BuildSelectClause(string entityName)
    {
      var entity = FindEntity(entityName);
      return $"SELECT * FROM {entity.SchemaName.ToLower()}.{entity.Name.ToLower()} AS t_{entity.Name.ToLower()}";
    }

    public override string BuildJoinClause(string includeString, string entityName)
    {
      if (string.IsNullOrEmpty(includeString)) throw new ArgumentException(nameof(includeString));
      if (string.IsNullOrEmpty(entityName)) throw new ArgumentException(nameof(entityName));

      try
      {
        var entity = FindEntity(entityName);
        var tokens = GetIncludeTokens(includeString, entity);
        var parser = new PostgreSqlIncludeTokenParser(tokens);
        return parser.Parse();
      }
      catch (Exception ex)
      {
        throw new ArgumentException($"Invalid include string: {ex.Message}", ex);
      }
    }

    public override string BuildWhereClause(string filterString, string entityName)
    {
      try
      {
        var entity = FindEntity(entityName);
        var tokens = GetFilterTokens(filterString, entity);
        var parser = new PostgreSqlFilterTokenParser(tokens);
        var result = parser.Parse();
        return $"WHERE {result}";
      }
      catch (ArgumentException ex)
      {
        throw new ArgumentException($"Invalid filter string: {ex.Message}", ex);
      }
    }

    public override string BuildOrderByClause(string sort, string pkName, string entityName)
    {
      try
      {
        var entity = FindEntity(entityName);
        var tokens = GetSortTokens(sort, entity);
        var parser = new PostgreSqlSortTokenParser(tokens);
        var orderby = parser.Parse();
        return $"ORDER BY {orderby}";
      }
      catch (ArgumentException ex)
      {
        throw new ArgumentException($"Invalid sort string: {ex.Message}", ex);
      }
    }

    public override string BuildLimitOffsetClause(int limit, int offset)
    {
      try
      {
        var token = new PagingToken(limit, offset);
        var parser = new PostgreSqlPagingParser(token);
        return parser.Parse();
      }
      catch (ArgumentException ex)
      {
        throw new ArgumentException($"Invalid limit or offset: {ex.Message}", ex);
      }
    }
  }
}
