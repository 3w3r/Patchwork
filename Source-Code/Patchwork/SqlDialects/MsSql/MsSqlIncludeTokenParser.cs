using Patchwork.Expansion;

using System.Text;

namespace Patchwork.SqlDialects.MsSql;
public class MsSqlIncludeTokenParser : IncludeTokenParserBase
{
  public MsSqlIncludeTokenParser(List<IncludeToken> tokens) : base(tokens) { }

  /// <summary>
  /// Parses the list of include tokens and generates a SQL statement for joining related tables.
  /// </summary>
  /// <returns>The generated SQL statement as a string.</returns>
  public override string Parse()
  {
    // Initialize a string builder to build the SQL statement.
    var sb = new StringBuilder();

    // Iterate through each include token.
    foreach (var token in _tokens)
    {
      // If the child schema name is empty, use an empty string. Otherwise, format the schema name with square brackets.
      var cs = string.IsNullOrEmpty(token.ChildSchemaName) ? "" : $"[{token.ChildSchemaName}].";

      // Append a line to the SQL statement for each join condition.
      sb.AppendLine($"LEFT OUTER JOIN {cs}[{token.ChildTableName}] AS [t_{token.ChildTableName}] ON " +
                    $"[t_{token.ParentTableName}].[{token.ParentTableFkName}] = " +
                    $"[t_{token.ChildTableName}].[{token.ChildTablePkName}]");
    }

    // Return the generated SQL statement as a string.
    return sb.ToString();
  }
}
