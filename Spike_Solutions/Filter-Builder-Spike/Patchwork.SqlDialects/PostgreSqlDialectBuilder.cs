using System;
using Patchwork.Filters;
using Patchwork.Sort;
using Patchwork.Paging;

namespace Patchwork.SqlDialects
{
  public class PostgreSqlDialectBuilder : ISqlDialectBuilder
  {
    public string BuildSelectClause(string tableName, string schemaName)
    {
      return $"SELECT * FROM {schemaName}.{tableName}";
    }

    public string BuildWhereClause(string filterString)
    {
      if (string.IsNullOrWhiteSpace(filterString))
        throw new ArgumentException("No input string");

      try
      {
        var lexer = new Lexer(filterString);
        var tokens = lexer.Tokenize();

        if (tokens.Count == 0)
          throw new ArgumentException("No valid tokens found");

        var parser = new PostgreSqlTokenParser(tokens);
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

    public string BuildLimitOffsetClause(int limit, int offset)
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
