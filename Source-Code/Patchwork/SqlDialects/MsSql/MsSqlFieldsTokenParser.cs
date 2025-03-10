using System.Text;
using Patchwork.Fields;

namespace Patchwork.SqlDialects.MsSql;

public class MsSqlFieldsTokenParser : SqlFieldsTokenParserBase
{
  public MsSqlFieldsTokenParser(List<FieldsToken> tokens) : base(tokens) { }

  public override string Parse()
  {
    StringBuilder sb = new StringBuilder();
    foreach (FieldsToken token in _tokens)
    {
      if (!string.IsNullOrEmpty(token.Prefix))
        sb.Append($"[{token.Prefix}].");
      sb.Append($"[{token.Name}], ");
    }
    return sb.ToString().Trim().TrimEnd(',');
  }
}
