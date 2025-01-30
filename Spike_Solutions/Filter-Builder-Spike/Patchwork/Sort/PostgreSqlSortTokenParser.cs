using System.Text;

namespace Patchwork.Sort;

public class PostgreSqlSortTokenParser
{
  private List<SortToken> _tokens;
  public PostgreSqlSortTokenParser(List<SortToken> tokens)
  {
    _tokens = tokens;
  }
  public string Parse()
  {
    StringBuilder orderByClause = new StringBuilder();
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
      return $"t_{token.EntityName.ToLower()}.{token.Column.ToLower()}";
    else
      return $"t_{token.EntityName.ToLower()}.{token.Column.ToLower()} desc";
  }
}
