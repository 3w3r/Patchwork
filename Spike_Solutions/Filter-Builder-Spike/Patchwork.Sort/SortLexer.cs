using System;
using System.Collections.Generic;
using System.Linq;

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
