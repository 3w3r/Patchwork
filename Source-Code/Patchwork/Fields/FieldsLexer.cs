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

  /// <summary>
  /// Tokenizes the input string representing fields for an entity.
  /// </summary>
  /// <returns>A list of <see cref="FieldsToken"/> representing the fields.</returns>
  /// <exception cref="ArgumentException">Thrown when a field in the input string is not valid for the entity.</exception>
  public List<FieldsToken> Tokenize()
  {
    // If the input string is "*", return a list containing a single token representing all fields.
    if (_input.Trim().Equals("*"))
      return _allFields;

    // Initialize a list to store the tokens.
    List<FieldsToken> tokens = new List<FieldsToken>();

    // Split the input string by commas and iterate over each segment.
    foreach (string segment in _input.Trim().Split(','))
    {
      // Skip empty segments.
      if (string.IsNullOrEmpty(segment))
        continue;

      // Read the identifier from the segment.
      string child = ReadIdentifier(segment.Trim());

      // Find the corresponding column in the entity's columns collection.
      Column? col = _entity.Columns.FirstOrDefault(e => e.Name.Equals(child, StringComparison.OrdinalIgnoreCase));

      // If the column is not found, throw an exception.
      if (col == null)
        throw new ArgumentException($"The field {child} is not valid for {_entity.Name}.");

      // Add a token representing the column to the list.
      tokens.Add(new FieldsToken($"t_{_entity.Name}", col.Name));
    }

    // Return the list of tokens.
    return tokens;
  }

  private string ReadIdentifier(string segment)
  {
    // Initialize the position to the start of the segment.
    int position = 0;

    // Initialize a StringBuilder to build the identifier.
    StringBuilder sb = new StringBuilder();

    // Loop through each character in the segment.
    while (
      // Continue until we reach the end of the segment.
      position < segment.Length
      && (
        // Add the character to the identifier if it is a letter, digit, underscore, or dot.
        char.IsLetterOrDigit(segment[position])
        || _input[position] == '_'
        || _input[position] == '.'
      )
    )
    {
      // Append the current character to the StringBuilder.
      sb.Append(segment[position++]);
    }

    // Return the built identifier as a string.
    return sb.ToString();
  }
}
