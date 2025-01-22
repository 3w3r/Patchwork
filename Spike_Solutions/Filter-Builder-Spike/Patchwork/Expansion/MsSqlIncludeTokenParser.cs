using System.Text;
using Patchwork.DbSchema;

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
    var sb = new StringBuilder();
    foreach (var token in _tokens)
    {
      sb.AppendLine($"LEFT OUTER JOIN [{token.ChildTableName}] AS {token.ChildTablePrefixName} ON " +
                    $"[{token.ParentTablePrefixName}].[{token.ParentTableFkName}] = " +
                    $"[{token.ChildTablePrefixName}].[{token.ChildTablePkName}]");

    }
    return sb.ToString();
  }
}
