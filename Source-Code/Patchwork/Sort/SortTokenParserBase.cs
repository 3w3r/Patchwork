using System.Text;

namespace Patchwork.Sort;

public abstract class SortTokenParserBase
{
  protected readonly List<SortToken> _tokens;
  public SortTokenParserBase(List<SortToken> tokens)
  {
    _tokens = tokens;
  }

  /// <summary>
  /// Parses the list of sort tokens and generates an order by clause string.
  /// </summary>
  /// <returns>A string representing the order by clause.</returns>
  public string Parse()
  {
    // Initialize a StringBuilder to store the order by clause
    StringBuilder orderByClause = new StringBuilder();

    // Call the ParseExpression method to parse the tokens and append the rendered tokens to the StringBuilder
    ParseExpression(orderByClause);

    // Convert the StringBuilder to a string and return the order by clause
    return orderByClause.ToString();
  }
  private void ParseExpression(StringBuilder sb)
  {
    // Iterate through each token in the list of tokens
    for (int i = 0; i < _tokens.Count; i++)
    {
      // Append the rendered token to the StringBuilder
      sb.Append(RenderToken(_tokens[i]));

      // If the current token is not the last token in the list, append a comma and a space
      if (i < _tokens.Count - 1)
        sb.Append(", ");
    }
  }

  public abstract string RenderToken(SortToken token);
}
