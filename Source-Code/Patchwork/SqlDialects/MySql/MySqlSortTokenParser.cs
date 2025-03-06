using Patchwork.Sort;

namespace Patchwork.SqlDialects.MySql;

public class MySqlSortTokenParser : SortTokenParserBase
{
  public MySqlSortTokenParser(List<SortToken> tokens) : base(tokens) { }

  public override string RenderToken(SortToken token)
  {
    if (token.Direction == SortDirection.Ascending)
      return $"t_{token.EntityName}.{token.Column}";
    else
      return $"t_{token.EntityName}.{token.Column} DESC";
  }
}
