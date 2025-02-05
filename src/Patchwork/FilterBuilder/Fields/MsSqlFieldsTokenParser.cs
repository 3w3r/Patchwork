using System.Text;

namespace Patchwork.Fields;

public class MsSqlFieldsTokenParser
{
  private readonly List<FieldsToken> _tokens;

  public MsSqlFieldsTokenParser(List<FieldsToken> tokens)
  {
    _tokens = tokens;
  }
  public string Parse()
  {
    StringBuilder sb = new StringBuilder();
    foreach (FieldsToken token in _tokens)
    {
      if (!string.IsNullOrEmpty(token.Prefix))
        sb.Append($"[{token.Prefix}].");
      sb.Append($"[{token.Name}], ");
    }
    string final = sb.ToString().Trim().TrimEnd(',');
    return final;
  }
}
