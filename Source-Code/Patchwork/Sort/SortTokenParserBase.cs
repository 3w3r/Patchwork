using System.Text;

namespace Patchwork.Sort;

public abstract class SortTokenParserBase
{
  protected readonly List<SortToken> _tokens;
  public SortTokenParserBase(List<SortToken> tokens)
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

  public abstract string RenderToken(SortToken token);
}
