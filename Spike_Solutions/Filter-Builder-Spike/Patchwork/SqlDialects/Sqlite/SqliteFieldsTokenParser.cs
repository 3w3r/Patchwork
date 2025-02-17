using System.Text;
using Patchwork.Fields;

namespace Patchwork.SqlDialects.Sqlite;

public class SqliteFieldsTokenParser : SqlFieldsTokenParserBase
{
  public SqliteFieldsTokenParser(List<FieldsToken> tokens) : base(tokens) { }

  public override string Parse()
  {
    StringBuilder sb = new StringBuilder();
    foreach (FieldsToken token in _tokens)
    {
      if (!string.IsNullOrEmpty(token.Prefix))
        sb.Append($"{token.Prefix.ToLower()}.");
      sb.Append($"{token.Name.ToLower()}, ");
    }
    return sb.ToString().Trim().TrimEnd(',');
  }
}
