using System.Text;
using Patchwork.Expansion;

namespace Patchwork.SqlDialects.MySql;

public class MySqlIncludeTokenParser : IncludeTokenParserBase
{
  public MySqlIncludeTokenParser(List<IncludeToken> tokens) : base(tokens) { }

  public override string Parse()
  {
    StringBuilder sb = new StringBuilder();
    foreach (IncludeToken token in _tokens)
    {
      sb.AppendLine($"LEFT OUTER JOIN `{token.ChildSchemaName}`.`{token.ChildTableName}` AS t_{token.ChildTableName} ON " +
                    $"t_{token.ParentTableName}.`{token.ParentTableFkName}` = " +
                    $"t_{token.ChildTableName}.`{token.ChildTablePkName}`");

    }
    return sb.ToString();
  }
}
