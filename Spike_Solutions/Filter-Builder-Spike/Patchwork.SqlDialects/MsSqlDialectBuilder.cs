using System;
using Patchwork.Filters;
using Patchwork.Sort;
using Patchwork.Paging;

namespace Patchwork.SqlDialects
{
  public class MsSqlDialectBuilder : ISqlDialectBuilder
  {
    public string BuildSelectClause(string tableName, string schemaName)
    {
      return $"SELECT * FROM [{schemaName}].[{tableName}]";
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
