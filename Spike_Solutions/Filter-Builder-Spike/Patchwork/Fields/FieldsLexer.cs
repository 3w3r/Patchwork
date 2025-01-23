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
    if (_input.Trim().Equals("*")) return _allFields;

    var tokens = new List<FieldsToken>();
    foreach (var segment in _input.Trim().Split(','))
    {
      var child = ReadIdentifier(segment);
      var col = _entity.Columns.FirstOrDefault(e => e.Name.Equals(child, StringComparison.OrdinalIgnoreCase));
      if (col == null)
        throw new ArgumentException($"The field {child} is not valid for {_entity.Name}.");

      tokens.Add(new FieldsToken($"T_{_entity.Name}", col.Name));
    }

    return tokens;
  }

  private string ReadIdentifier(string segment)
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
    return value;
  }
}
