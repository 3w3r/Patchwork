using Patchwork.Sort;

namespace Patchwork.SqlDialects.MySql;

public class MySqlSortTokenParser : SortTokenParserBase
{
  public MySqlSortTokenParser(List<SortToken> tokens) : base(tokens) { }

  /// <summary>
  /// Renders a single <see cref="SortToken"/> into a MySQL-compatible sort clause.
  /// </summary>
  /// <param name="token">The <see cref="SortToken"/> to render.</param>
  /// <returns>A string representing the rendered sort clause.</returns>
  public override string RenderToken(SortToken token)
  {
    // If the sort direction is ascending, return the column name with the table alias
    if (token.Direction == SortDirection.Ascending)
      return $"t_{token.EntityName}.{token.Column}";
    // If the sort direction is descending, return the column name with the table alias and the "DESC" keyword
    else
      return $"t_{token.EntityName}.{token.Column} DESC";
  }
}
