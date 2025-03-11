using Patchwork.Expansion;

using System.Text;

namespace Patchwork.SqlDialects.MySql;

public class MySqlIncludeTokenParser : IncludeTokenParserBase
{
  public MySqlIncludeTokenParser(List<IncludeToken> tokens) : base(tokens) { }

  /// <summary>
  /// Parses the list of <see cref="IncludeToken"/> objects and generates a MySQL-compatible JOIN clause.
  /// </summary>
  /// <returns>A string representing the JOIN clause.</returns>
  public override string Parse()
  {
    // Initialize a StringBuilder to build the JOIN clause
    var sb = new StringBuilder();

    // Iterate through each IncludeToken in the list
    foreach (var token in _tokens)
    {
      // If the ChildSchemaName is null or empty, use an empty string; otherwise, include the schema name with a dot
      var cs = string.IsNullOrEmpty(token.ChildSchemaName) ? "" : $"{token.ChildSchemaName}.";

      // Append a LEFT OUTER JOIN clause to the StringBuilder, using the schema name, child table name, and foreign key/primary key relationships
      sb.AppendLine($"LEFT OUTER JOIN {cs}{token.ChildTableName} AS t_{token.ChildTableName} ON " +
                    $"t_{token.ParentTableName}.{token.ParentTableFkName} = " +
                    $"t_{token.ChildTableName}.{token.ChildTablePkName}");
    }

    // Return the generated JOIN clause as a string
    return sb.ToString();
  }
}
