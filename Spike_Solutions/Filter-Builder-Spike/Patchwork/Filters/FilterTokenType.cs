namespace Patchwork.Filters;

[Flags]
public enum FilterTokenType
{
  UUID       = 0b00000010_00000000, // Unique Identifier
  Identifier = 0b00000001_00000000, // Column names
  Operator   = 0b00000000_10000000, // eq, ne, gt, ge, lt, le, in, ct, sw
  Numeric    = 0b00000000_01000000, // Numeric values
  Textual    = 0b00000000_00100000, // Textual values
  DateTime   = 0b00000000_00010000, // DateTime values
  Logical    = 0b00000000_00001000, // AND, OR
  OpenParen  = 0b00000000_00000100, // (
  CloseParen = 0b00000000_00000010, // )
  Whitespace = 0b00000000_00000001, // Whitespace
  Invalid    = 0b00000000_00000000, // Invalid character or illogical operator

  Parenthesis = OpenParen | CloseParen,
  Value = Numeric | Textual | DateTime | UUID,
}
