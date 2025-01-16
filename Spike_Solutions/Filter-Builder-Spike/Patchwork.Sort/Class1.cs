using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Patchwork.Sort;

public class SortLexer
{
  private string _input;

  public SortLexer(string input)
  {
    _input = input;
  }

  public List<SortToken> Tokenize()
  {
    var tokens = new List<SortToken>();
    var split = _input.Trim().Split(',');
    foreach (var token in split)
    {
      var tokenValue = token.Trim();
      var t = MakeValidToken(tokenValue);
      tokens.Add(t);
    }

    return tokens;
  }
  public SortToken MakeValidToken(string columnSort)
  {
    if (!char.IsLetter(columnSort.First()))
      throw new ArgumentException("Column name must begin with a letter.");

    if (columnSort.Contains(':'))
      return MakeSortedColumnToken(columnSort);
    else return MakeAscendingColumnToken(columnSort);
  }

  private SortToken MakeAscendingColumnToken(string columnSort)
  {
    ValidateColumnName(columnSort);
    return new SortToken(columnSort, SortDirection.Ascending);
  }

  private static void ValidateColumnName(string columnSort)
  {
    foreach (var c in columnSort.ToArray())
    {
      if (char.IsLetter(c))
        continue;
      if (char.IsDigit(c))
        continue;
      if (c == '_')
        continue;
      throw new ArgumentException($"Invalid character found in column name: {columnSort}");
    }
  }

  private SortToken MakeSortedColumnToken(string columnSort)
  {
    var split = columnSort.Split(':');
    if (split.Length != 2)
      throw new ArgumentException("Invalid sort direction. Use only column names and 'asc' or 'desc'.");
    var name = split[0].Trim();
    ValidateColumnName(name);

    var direction = SortDirection.Ascending;
    switch (split[1].Trim().ToLower())
    {
      case "asc":
        break;
      case "desc":
        direction = SortDirection.Descending;
        break;
      default:
        throw new ArgumentException($"Invalid sort order: {split[1].Trim()} is not asc or desc.");
    }

    return new SortToken(name, direction);
  }
}

public class SortToken
{
  public SortToken(string column, SortDirection direction)
  {
    Column = column;
    Direction = direction;
  }
  public string Column { get; set; } = string.Empty;
  public SortDirection Direction { get; set; } = SortDirection.Ascending;
}

public enum SortDirection
{
  Ascending, Descending
}

public class PostgreSqlSortTokenParser
{
  private List<SortToken> _tokens;
  public PostgreSqlSortTokenParser(List<SortToken> tokens)
  {
    _tokens = tokens;
  }
  public string Parse()
  {
    var orderByClause = new StringBuilder();
    ParseExpression(orderByClause);
    return orderByClause.ToString();
  }

  private void ParseExpression(StringBuilder sb)
  {
    for (int i = 0; i < _tokens.Count; i++)
    {
      sb.Append(RenderToken(_tokens[i]));
      if (i < _tokens.Count - 1)
        sb.Append(", ");
    }
  }

  public string RenderToken(SortToken token)
  {
    if (token.Direction == SortDirection.Ascending)
      return $"{token.Column.ToLower()}";
    else
      return $"{token.Column.ToLower()} desc";
  }
}

public class MsSortTokenParser
{
  private List<SortToken> _tokens;
  public MsSortTokenParser(List<SortToken> tokens)
  {
    _tokens = tokens;
  }
  public string Parse()
  {
    var orderByClause = new StringBuilder();
    ParseExpression(orderByClause);
    return orderByClause.ToString();
  }

  private void ParseExpression(StringBuilder sb)
  {
    for (int i = 0; i < _tokens.Count; i++)
    {
      sb.Append(RenderToken(_tokens[i]));
      if (i < _tokens.Count - 1)
        sb.Append(", ");
    }
  }

  public string RenderToken(SortToken token)
  {
    if (token.Direction == SortDirection.Ascending)
      return $"[{token.Column}]";
    else
      return $"[{token.Column}] DESC";
  }
}
