using System.Text;
using Patchwork.DbSchema;

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
    var sb = new StringBuilder();
    foreach (var token in _tokens)
    {
      if (!string.IsNullOrEmpty(token.Prefix)) sb.Append($"[{token.Prefix}].");
      sb.Append($"[{token.Name}], ");
    }
    var final = sb.ToString().Trim().TrimEnd(',');
    return final;
  }
}
