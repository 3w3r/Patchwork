using System.Text;
using Patchwork.DbSchema;

namespace Patchwork.Fields;

public class FieldsLexer
{
  private readonly string _input;
  private readonly Entity _entity;
  private readonly DatabaseMetadata _meta;

  private static readonly List<FieldsToken> _allFields = new List<FieldsToken>() { new FieldsToken("", "*") };
  public FieldsLexer(string input, Entity entity, DatabaseMetadata meta)
  {
    _input = input;
    _entity = entity;
    _meta = meta;
  }

  public List<FieldsToken> Tokenize()
  {
    if (_input.Trim().Equals("*"))
      return _allFields;

    List<FieldsToken> tokens = new List<FieldsToken>();
    foreach (string segment in _input.Trim().Split(','))
    {
      if (string.IsNullOrEmpty(segment))
        continue;
      string child = ReadIdentifier(segment.Trim());
      Column? col = _entity.Columns.FirstOrDefault(e => e.Name.Equals(child, StringComparison.OrdinalIgnoreCase));
      if (col == null)
        throw new ArgumentException($"The field {child} is not valid for {_entity.Name}.");

      tokens.Add(new FieldsToken($"T_{_entity.Name}", col.Name));
    }

    return tokens;
  }

  private string ReadIdentifier(string segment)
  {
    int position = 0;
    StringBuilder sb = new StringBuilder();
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
    string value = sb.ToString();
    return value;
  }
}
