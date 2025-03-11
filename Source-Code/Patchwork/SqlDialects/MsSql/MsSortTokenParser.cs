using Patchwork.Sort;

namespace Patchwork.SqlDialects.MsSql;

public class MsSortTokenParser : SortTokenParserBase
{
  public MsSortTokenParser(List<SortToken> tokens) : base(tokens) { }

  /// <summary>
  /// Renders a SortToken into a SQL-compatible string for sorting.
  /// </summary>
  /// <param name="token">The SortToken to render.</param>
  /// <returns>A SQL-compatible string representing the SortToken.</returns>
  public override string RenderToken(SortToken token)
  {
    // If the SortToken's direction is ascending, return the column name in the format: [t_EntityName].[ColumnName]
    if (token.Direction == SortDirection.Ascending)
      return $"[t_{token.EntityName}].[{token.Column}]";
    // If the SortToken's direction is descending, return the column name in the format: [t_EntityName].[ColumnName] DESC
    else
      return $"[t_{token.EntityName}].[{token.Column}] DESC";
  }
}
