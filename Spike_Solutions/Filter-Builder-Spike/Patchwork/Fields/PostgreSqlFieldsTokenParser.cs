using System.Text;

namespace Patchwork.Fields;

public class PostgreSqlFieldsTokenParser
{
  private readonly List<FieldsToken> _tokens;

  public PostgreSqlFieldsTokenParser(List<FieldsToken> tokens)
  {
    _tokens = tokens;
  }
  public string Parse()
  {
    StringBuilder sb = new StringBuilder();
    foreach (FieldsToken token in _tokens)
    {
      if (!string.IsNullOrEmpty(token.Prefix))
        sb.Append($"{token.Prefix.ToLower()}.");
      sb.Append($"{token.Name.ToLower()}, ");
    }
    string final = sb.ToString().Trim().TrimEnd(',');
    return final;
  }
}
