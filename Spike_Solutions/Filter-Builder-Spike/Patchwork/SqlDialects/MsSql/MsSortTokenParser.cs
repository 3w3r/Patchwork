using Patchwork.Sort;

namespace Patchwork.SqlDialects.MsSql;

public class MsSortTokenParser : SortTokenParserBase
{
  public MsSortTokenParser(List<SortToken> tokens) : base(tokens) { }

  public override string RenderToken(SortToken token)
  {
    if (token.Direction == SortDirection.Ascending)
      return $"[t_{token.EntityName}].[{token.Column}]";
    else
      return $"[t_{token.EntityName}].[{token.Column}] DESC";
  }
}
