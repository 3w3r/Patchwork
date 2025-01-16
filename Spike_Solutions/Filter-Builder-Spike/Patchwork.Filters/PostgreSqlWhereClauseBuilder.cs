using System;

namespace Patchwork.Filters
{
  public static class PostgreSqlDialectBuilder
  {
    public static string BuildSelectClause(string tableName, string schemaName)
    {
      return $"SELECT * FROM {schemaName}.{tableName}";
    }

    public static string BuildWhereClause(string filterString)
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
  }
}
