using Patchwork.Sort;

namespace Patchwork.SqlDialects.Sqlite;

public class SqliteSortTokenParser : SortTokenParserBase
{
  public SqliteSortTokenParser(List<SortToken> tokens) : base(tokens) { }

  public override string RenderToken(SortToken token)
  {
    if (token.Direction == SortDirection.Ascending)
      return $"t_{token.EntityName.ToLower()}.{token.Column.ToLower()}";
    else
      return $"t_{token.EntityName.ToLower()}.{token.Column.ToLower()} desc";
  }
}
