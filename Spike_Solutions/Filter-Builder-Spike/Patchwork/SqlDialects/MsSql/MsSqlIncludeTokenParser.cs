using System.Text;
using Patchwork.Expansion;

namespace Patchwork.SqlDialects.MsSql;
public class MsSqlIncludeTokenParser : IncludeTokenParserBase
{
  public MsSqlIncludeTokenParser(List<IncludeToken> tokens) : base(tokens) { }

  public override string Parse()
  {
    StringBuilder sb = new StringBuilder();
    foreach (IncludeToken token in _tokens)
    {
      var cs = string.IsNullOrEmpty(token.ChildSchemaName) ? "" : $"[{token.ChildSchemaName}].";

      sb.AppendLine($"LEFT OUTER JOIN {cs}[{token.ChildTableName}] AS [T_{token.ChildTableName}] ON " +
                    $"[T_{token.ParentTableName}].[{token.ParentTableFkName}] = " +
                    $"[T_{token.ChildTableName}].[{token.ChildTablePkName}]");
    }
    return sb.ToString();
  }
}
