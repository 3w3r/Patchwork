using Patchwork.Fields;

using System.Text;

namespace Patchwork.SqlDialects.MySql;

public class MySqlFieldsTokenParser : SqlFieldsTokenParserBase
{
  public MySqlFieldsTokenParser(List<FieldsToken> tokens) : base(tokens) { }

  /// <summary>
  /// Parses a list of <see cref="FieldsToken"/> objects into a MySQL-compatible string representation.
  /// </summary>
  /// <returns>A string containing the parsed field names, separated by commas.</returns>
  public override string Parse()
  {
    // Initialize a StringBuilder to build the result string
    var sb = new StringBuilder();

    // Iterate through each FieldsToken in the list
    foreach (var token in _tokens)
    {
      // If the token has a prefix, append it to the StringBuilder followed by a dot
      if (!string.IsNullOrEmpty(token.Prefix))
        sb.Append($"{token.Prefix}.");

      // Append the token's name to the StringBuilder followed by a comma and a space
      sb.Append($"{token.Name}, ");
    }

    // Trim any trailing whitespace and comma from the StringBuilder
    // and return the resulting string
    return sb.ToString().Trim().TrimEnd(',');
  }
}
