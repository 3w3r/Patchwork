using System.Text;

namespace Patchwork.Expansion;

public class MsSqlIncludeTokenParser
{
  private readonly List<IncludeToken> _tokens;

  public MsSqlIncludeTokenParser(List<IncludeToken> tokens)
  {
    _tokens = tokens;
  }
  public string Parse()
  {
    StringBuilder sb = new StringBuilder();
    foreach (IncludeToken token in _tokens)
    {
      sb.AppendLine($"LEFT OUTER JOIN [{token.ChildSchemaName}].[{token.ChildTableName}] AS [T_{token.ChildTableName}] ON " +
                    $"[T_{token.ParentTableName}].[{token.ParentTableFkName}] = " +
                    $"[T_{token.ChildTableName}].[{token.ChildTablePkName}]");

    }
    return sb.ToString();
  }
}
