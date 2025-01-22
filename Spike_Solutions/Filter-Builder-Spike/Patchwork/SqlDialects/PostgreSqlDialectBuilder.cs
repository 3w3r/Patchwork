using System;
using Patchwork.Filters;
using Patchwork.Sort;
using Patchwork.Paging;
using Patchwork.DbSchema;
using Npgsql;
using Patchwork.Expansion;
using System.Data.Common;

namespace Patchwork.SqlDialects
{
  public class PostgreSqlDialectBuilder : SqlDialectBuilderBase
  {

    public PostgreSqlDialectBuilder(string connectionString) : base(connectionString) { }
    public PostgreSqlDialectBuilder(DatabaseMetadata metadata) : base(metadata) { }

    protected override DbConnection GetConnection()
    {
      return new NpgsqlConnection(_connectionString);
    }

    public override string BuildSelectClause(string entityName)
    {
      var entity = _metadata.Schemas.SelectMany(x => x.Tables).FirstOrDefault(t => t.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));
      if (entity == null) throw new ArgumentException($"Invalid Table Name: {entityName}");
      return $"SELECT * FROM {entity.SchemaName.ToLower()}.{entity.Name.ToLower()} AS t_{entity.Name.ToLower()}";
    }

    public override string BuildJoinClause(string includeString, string entityName)
    {
      if (string.IsNullOrEmpty(includeString)) return "";

      try
      {
        DiscoverSchema();
        if (_metadata == null) throw new ArgumentException("Cannot access database schema");
        var entity = _metadata.Schemas.SelectMany(x => x.Tables).FirstOrDefault(t => t.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));
        if (entity == null) throw new ArgumentException($"Invalid Table Name: {entityName}");

        var lexer = new IncludeLexer(includeString, entity, _metadata);
        var tokens = lexer.Tokenize();
        var parser = new PostgreSqlIncludeTokenParser(tokens);
        var result = parser.Parse();
        return result.ToString();
      }
      catch (Exception ex)
      {
        throw new ArgumentException($"Invalid include string: {ex.Message}", ex);
      }
    }

    public override string BuildWhereClause(string filterString)
    {
      if (string.IsNullOrWhiteSpace(filterString))
        throw new ArgumentException("No input string");

      try
      {
        var lexer = new FilterLexer(filterString);
        var tokens = lexer.Tokenize();

        if (tokens.Count == 0)
          throw new ArgumentException("No valid tokens found");

        var parser = new PostgreSqlFilterTokenParser(tokens);
        var result = parser.Parse();
        return $"WHERE {result}";
      }
      catch (ArgumentException ex)
      {
        throw new ArgumentException($"Invalid filter string: {ex.Message}", ex);
      }
    }

    public override string BuildOrderByClause(string sort, string pkName)
    {
      try
      {
        var lexer = new SortLexer(sort);
        var tokens = lexer.Tokenize();
        var parser = new PostgreSqlSortTokenParser(tokens);
        var orderby = parser.Parse();
        if (string.IsNullOrWhiteSpace(orderby))
          return $"ORDER BY {pkName}";
        else
          return $"ORDER BY {orderby}, {pkName}";
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
