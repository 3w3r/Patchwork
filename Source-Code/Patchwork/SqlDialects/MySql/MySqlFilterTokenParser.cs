using Patchwork.Filters;

using System.Text;

namespace Patchwork.SqlDialects.MySql;

public class MySqlFilterTokenParser : FilterTokenParserBase
{
  public MySqlFilterTokenParser(List<FilterToken> tokens) : base(tokens) { }

  /// <summary>
  /// Parses a list of <see cref="FilterToken"/> objects into a MySQL-compatible string representation.
  /// This method handles nested expressions, logical operators (AND, OR), and simple expressions.
  /// </summary>
  /// <param name="sb">The StringBuilder to append the parsed string to.</param>
  protected override void ParseExpression(StringBuilder sb)
  {
    // Check if there are no more tokens to parse
    if (_position >= _tokens.Count)
      return;

    // Handle opening parenthesis
    if (_tokens[_position].Type == FilterTokenType.OpenParen)
    {
      sb.Append("(");
      _position++;
      ParseExpression(sb);

      // Check for closing parenthesis
      if (_position < _tokens.Count && _tokens[_position].Type == FilterTokenType.CloseParen)
      {
        sb.Append(")");
        _position++;
      } else
      {
        throw new ArgumentException("Unmatched parenthesis");
      }
    } else
    {
      // Parse a simple expression
      ParseSimpleExpression(sb);
    }

    // Check for logical operator (AND, OR)
    if (_position < _tokens.Count &&
        (_tokens[_position].Value.Equals("AND", StringComparison.OrdinalIgnoreCase)
         || _tokens[_position].Value.Equals("OR", StringComparison.OrdinalIgnoreCase)))
    {
      sb.Append(" ").Append(_tokens[_position].Value.ToUpper()).Append(" ");
      _position++;
      ParseExpression(sb);
    }
    // Check for closing parenthesis
    else if (_position < _tokens.Count && _tokens[_position].Type != FilterTokenType.CloseParen)
    {
      throw new ArgumentException("Expected logical operator or closing parenthesis");
    }
  }

  private void ParseSimpleExpression(StringBuilder sb)
  {
    // Retrieve the identifier token and check if it is of type Identifier
    var identifier = _tokens[_position++];
    if (identifier.Type != FilterTokenType.Identifier)
      throw new ArgumentException("Expected identifier");

    // Check if there are no more tokens to parse after the identifier
    if (_position >= _tokens.Count)
      throw new ArgumentException("Expected operator after identifier");

    // Retrieve the operator token and check if it is of type Operator
    var op = _tokens[_position++];
    if (op.Type != FilterTokenType.Operator)
      throw new ArgumentException("Expected operator");

    // Check if there are no more tokens to parse after the operator
    if (_position >= _tokens.Count)
      throw new ArgumentException("Expected value after operator");

    // Retrieve the value token
    var value = _tokens[_position++];

    // Handle case where value is an open parenthesis when operator is 'in'
    // and check if the operator is 'in' and the value is not of type OpenParen
    if (value.Type != FilterTokenType.OpenParen && op.Type == FilterTokenType.Operator && op.Value == "in")
    {
      throw new ArgumentException("Expected open paren to begin list of acceptable values");
    }
    // If the operator is not 'in' and the value is not of type Value, throw an ArgumentException
    else if (op.Value != "in" && !FilterTokenType.Value.HasFlag(value.Type))
    {
      throw new ArgumentException("Expected value");
    }

    // Append the entity name, identifier value, and converted operator to the StringBuilder
    sb.Append($"t_{identifier.EntityName}.{identifier.Value} {ConvertOperator(op.Value)} ");

    // If the operator is 'in', handle the list of acceptable values
    if (op.Value == "in")
    {
      sb.Append("(");

      // Iterate through the tokens until a CloseParen is found or the end of tokens is reached
      while (_position < _tokens.Count && FilterTokenType.Value.HasFlag(_tokens[_position].Type))
      {
        // Append the parameter name of the current token to the StringBuilder
        sb.Append($"@{_tokens[_position].ParameterName}");
        _position++;

        // If the next token is not a CloseParen, append a comma and a space
        if (_position < _tokens.Count && _tokens[_position].Type != FilterTokenType.CloseParen)
        {
          sb.Append(", ");
        }
      }

      sb.Append(")");
    }
    // If the operator is not 'in', handle the single value
    else
    {
      // If the value is of type Value, append the parameter name of the value to the StringBuilder
      if (FilterTokenType.Value.HasFlag(value.Type))
      {
        sb.Append($"@{value.ParameterName}");
      }
      // If the value is not of type Value, append the value itself to the StringBuilder
      else
      {
        sb.Append(value.Value);
      }
    }
  }

  private string ConvertOperator(string op)
  {
    // Switch statement to convert the operator string to the corresponding MySQL operator
    switch (op)
    {
      // If the operator is 'eq', return '='
      case "eq":
        return "=";

      // If the operator is 'ne', return '!='
      case "ne":
        return "!=";

      // If the operator is 'gt', return '>'
      case "gt":
        return ">";

      // If the operator is 'ge', return '>='
      case "ge":
        return ">=";

      // If the operator is 'lt', return '<'
      case "lt":
        return "<";

      // If the operator is 'le', return '<='
      case "le":
        return "<=";

      // If the operator is 'in', return 'IN'
      case "in":
        return "IN";

      // If the operator is 'ct' or 'sw', return 'LIKE'
      // Use ILIKE for case-insensitive pattern matching
      case "ct":
      case "sw":
        return "LIKE";

      // If the operator is not recognized, throw an ArgumentException
      default:
        throw new ArgumentException("Unknown operator");
    }
  }
}
