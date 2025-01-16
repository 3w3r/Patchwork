using System;

namespace Patchwork.Filters
{
  public static class MsSqlWhereClauseBuilder
  {
    public static string ConvertToSqlWhereClause(string filterString)
    {
      if (string.IsNullOrWhiteSpace(filterString))
        throw new ArgumentException("No input string");

      try
      {
        var lexer = new Lexer(filterString);
        var tokens = lexer.Tokenize();

        if (tokens.Count == 0)
          throw new ArgumentException("No valid tokens found");

        var parser = new MsSqlTokenParser(tokens);
        var result = parser.Parse();
        return $"WHERE {result}";
      }
      catch (ArgumentException ex)
      {
        throw new ArgumentException($"Invalid filter string: {ex.Message}", ex);
      }
    }
  }
}
