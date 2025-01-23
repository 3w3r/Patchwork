using System.ComponentModel;
using System.Text;
using Npgsql.Internal.Postgres;
using Patchwork.DbSchema;

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
    var sb = new StringBuilder();
    foreach (var token in _tokens)
    {
      if (!string.IsNullOrEmpty(token.Prefix)) sb.Append($"{token.Prefix.ToLower()}.");
      sb.Append($"{token.Name.ToLower()}, ");
    }
    var final = sb.ToString().Trim().TrimEnd(',');
    return final;
  }
}
