using Patchwork.Sort;

namespace Patchwork.SqlDialects.PostgreSql;
public class PostgreSqlSortTokenParser : SortTokenParserBase
{
  public PostgreSqlSortTokenParser(List<SortToken> tokens) : base(tokens) { }

  public override string RenderToken(SortToken token)
  {
    if (token.Direction == SortDirection.Ascending)
      return $"t_{token.EntityName.ToLower()}.{token.Column.ToLower()}";
    else
      return $"t_{token.EntityName.ToLower()}.{token.Column.ToLower()} desc";
  }
}
