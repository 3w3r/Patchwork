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
