using System;
using Patchwork.Filters;
using Patchwork.Sort;
using Patchwork.Paging;
using Patchwork.Schema;
using Microsoft.Data.SqlClient;
using Patchwork.Expansion;

namespace Patchwork.SqlDialects
{
  public class MsSqlDialectBuilder : ISqlDialectBuilder
  {
    private readonly string _connectionString;
    private DatabaseMetadata? _metadata = null;

    public MsSqlDialectBuilder(string connectionString)
    {
      _connectionString = connectionString;
    }

    public void DiscoverSchema()
    {
      if (_metadata != null) return;

      var schemaDiscoveryBuilder = new SchemaDiscoveryBuilder();
      using (var connection = new SqlConnection(_connectionString))
      {
        _metadata = schemaDiscoveryBuilder.ReadSchema(connection);
      }
    }

    public string BuildSelectClause(string tableName, string schemaName)
    {
      return $"SELECT * FROM [{schemaName}].[{tableName}]";
    }

    public string BuildJoinClause(string includeString, string entityName)
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
        var parser = new MsSqlIncludeTokenParser(tokens);
        var result = parser.Parse();
        return result.ToString();
      }
      catch (Exception ex)
      {
        throw new ArgumentException($"Invalid include string: {ex.Message}", ex);
      }
    }

    public string BuildWhereClause(string filterString)
    {
      if (string.IsNullOrWhiteSpace(filterString))
        throw new ArgumentException("No input string");

      try
      {
        var lexer = new FilterLexer(filterString);
        var tokens = lexer.Tokenize();

        if (tokens.Count == 0)
          throw new ArgumentException("No valid tokens found");

        var parser = new MsSqlFilterTokenParser(tokens);
        var result = parser.Parse();
        return $"WHERE {result}";
      }
      catch (ArgumentException ex)
      {
        throw new ArgumentException($"Invalid filter string: {ex.Message}", ex);
      }
    }

    public string BuildOrderByClause(string sort, string pkName)
    {
      try
      {
        var lexer = new SortLexer(sort);
        var tokens = lexer.Tokenize();
        var parser = new MsSortTokenParser(tokens);
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

    public string BuildLimitOffsetClause(int limit, int offset)
    {
      try
      {
        var token = new PagingToken(limit, offset);
        var parser = new MsSqlPagingParser(token);
        return parser.Parse();
      }
      catch (ArgumentException ex)
      {
        throw new ArgumentException($"Invalid limit or offset: {ex.Message}", ex);
      }
    }
  }
}
