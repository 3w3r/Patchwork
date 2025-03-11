using Patchwork.Fields;

using System.Text;

namespace Patchwork.SqlDialects.MsSql;

public class MsSqlFieldsTokenParser : SqlFieldsTokenParserBase
{
  public MsSqlFieldsTokenParser(List<FieldsToken> tokens) : base(tokens) { }

  /// <summary>
  /// Parses the list of <see cref="FieldsToken"/> objects and generates a SQL SELECT fields list.
  /// </summary>
  /// <returns>A string representing the parsed SQL SELECT fields list.</returns>
  public override string Parse()
  {
    // Initialize a new StringBuilder to construct the SQL SELECT fields list.
    var sb = new StringBuilder();

    // Iterate through each FieldsToken object in the _tokens list.
    foreach (var token in _tokens)
    {
      // If the token has a prefix, append it to the StringBuilder, enclosed in square brackets.
      if (!string.IsNullOrEmpty(token.Prefix))
        sb.Append($"[{token.Prefix}].");

      // Append the token's name to the StringBuilder, enclosed in square brackets.
      // Append a comma and a space after each token.
      sb.Append($"[{token.Name}], ");
    }

    // Trim any trailing whitespace and comma from the StringBuilder.
    // Return the resulting string.
    return sb.ToString().Trim().TrimEnd(',');
  }
}
