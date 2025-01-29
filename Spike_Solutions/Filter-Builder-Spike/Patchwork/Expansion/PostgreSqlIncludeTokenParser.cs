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
      sb.AppendLine($"LEFT OUTER JOIN {token.ChildSchemaName}.{token.ChildTableName} AS t_{token.ChildTableName} ON " +
                    $"t_{token.ParentTableName}.{token.ParentTableFkName} = " +
                    $"t_{token.ChildTableName}.{token.ChildTablePkName}");

    }
    return sb.ToString();
  }
}
