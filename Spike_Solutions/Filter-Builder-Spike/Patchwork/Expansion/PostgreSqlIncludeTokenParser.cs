using System.Text;

namespace Patchwork.Expansion;

public class PostgreSqlIncludeTokenParser
{
  private readonly List<IncludeToken> _tokens;

  public PostgreSqlIncludeTokenParser(List<IncludeToken> tokens)
  {
    _tokens = tokens;
  }
  public string Parse()
  {
    StringBuilder sb = new StringBuilder();
    foreach (IncludeToken token in _tokens)
    {
      sb.AppendLine($"LEFT OUTER JOIN {token.ChildTableName} AS {token.ChildTablePrefixName} ON " +
                    $"{token.ParentTablePrefixName}.{token.ParentTableFkName} = " +
                    $"{token.ChildTablePrefixName}.{token.ChildTablePkName}");

    }
    return sb.ToString();
  }
}
