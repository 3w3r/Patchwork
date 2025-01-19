using System.ComponentModel;
using System.Text;

namespace Patchwork.Expansion;

public class IncludeLexer
{
  private readonly string _input;
  public IncludeLexer(string input)
  {
    _input = input;
  }

  public List<IncludeToken> Tokenize()
  {
    var tokens = new List<IncludeToken>();
    foreach (var segment in _input.Trim().Split(','))
    {

    }
    return tokens;
  }
  private IncludeToken ReadIdentifier(string segment)
  {
    var position = 0;
    var sb = new StringBuilder();
    while (
      position < segment.Length
      && (char.IsLetterOrDigit(segment[position])
         || _input[position] == '_'
         || _input[position] == '.'
         )
      )
    {
      sb.Append(segment[position++]);
    }
    var value = sb.ToString();
    return new IncludeToken(value, "", "");
  }
}

public record IncludeToken(string Name, string ForeignKeyColumnName, string PrimaryKeyValue);

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
    return sb.ToString();
  }
}

public class PostgreSqlIncludeTokenParser
{
  private readonly List<IncludeToken> _tokens = new List<IncludeToken>();
  public PostgreSqlIncludeTokenParser(List<IncludeToken> tokens)
  {
    _tokens = tokens;
  }
  public string Parse()
  {
    var sb = new StringBuilder();
    return sb.ToString();
  }
}
