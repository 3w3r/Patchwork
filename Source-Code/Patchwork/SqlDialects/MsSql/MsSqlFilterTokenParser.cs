using Patchwork.Filters;

using System.Text;

namespace Patchwork.SqlDialects.MsSql;

public class MsSqlFilterTokenParser : FilterTokenParserBase
{
  public MsSqlFilterTokenParser(List<FilterToken> tokens) : base(tokens) { }

  /// <summary>
  /// Parses a filter expression and appends the corresponding SQL statement to the string builder.
  /// </summary>
  /// <param name="sb">The string builder to append the SQL statement to.</param>
  protected override void ParseExpression(StringBuilder sb)
  {
    // Check if there are no more tokens.
    if (_position >= _tokens.Count)
      return;

    // If the current token is an opening parenthesis, parse the nested expression.
    if (_tokens[_position].Type == FilterTokenType.OpenParen)
    {
      sb.Append("("); // Append the opening parenthesis to the SQL statement.
      _position++; // Move to the next token.
      ParseExpression(sb); // Recursively parse the nested expression.

      // Check if there is a closing parenthesis after the nested expression.
      if (_position < _tokens.Count && _tokens[_position].Type == FilterTokenType.CloseParen)
      {
        sb.Append(")"); // Append the closing parenthesis to the SQL statement.
        _position++; // Move to the next token.
      } else
      {
        throw new ArgumentException("Unmatched parenthesis"); // Throw an exception if there is no closing parenthesis.
      }
    } else
    {
      // If the current token is not an opening parenthesis, parse a simple expression.
      ParseSimpleExpression(sb);
    }

    // Check if there are more tokens and if the next token is a logical operator.
    if (_position < _tokens.Count &&
         (_tokens[_position].Value.Equals("AND", StringComparison.OrdinalIgnoreCase)
         || _tokens[_position].Value.Equals("OR", StringComparison.OrdinalIgnoreCase)
         )
       )
    {
      // Append the logical operator to the SQL statement.
      sb.Append(" ").Append(_tokens[_position].Value.ToUpper()).Append(" ");
      _position++; // Move to the next token.
      ParseExpression(sb); // Recursively parse the expression following the logical operator.
    }
    // Check if there are more tokens and if the next token is not a closing parenthesis.
    else if (_position < _tokens.Count && _tokens[_position].Type != FilterTokenType.CloseParen)
    {
      throw new ArgumentException("Expected logical operator or closing parenthesis"); // Throw an exception if the next token is not a logical operator or closing parenthesis.
    }
  }

  private void ParseSimpleExpression(StringBuilder sb)
  {
    // Parse a simple expression, which consists of an identifier, operator, and value.
    if (_position >= _tokens.Count)
      return;

    // Get the identifier token.
    var identifier = _tokens[_position++];
    if (identifier.Type != FilterTokenType.Identifier)
      throw new ArgumentException("Expected identifier");

    // Check if there's an operator token after the identifier.
    if (_position >= _tokens.Count)
      throw new ArgumentException("Expected operator after identifier");

    // Get the operator token.
    var op = _tokens[_position++];
    if (op.Type != FilterTokenType.Operator)
      throw new ArgumentException("Expected operator");

    // Check if there's a value token after the operator.
    if (_position >= _tokens.Count)
      throw new ArgumentException("Expected value after operator");

    // Get the value token.
    var value = _tokens[_position++];

    // need to handle case where value is open paren when operator is 'in'
    if (value.Type != FilterTokenType.OpenParen && op.Type == FilterTokenType.Operator && op.Value == "in")
    {
      throw new ArgumentException("Expected open paren to begin list of acceptable values");
    } else if (op.Value != "in" && !FilterTokenType.Value.HasFlag(value.Type))
    {
      throw new ArgumentException("Expected value");
    }

    // Append the identifier, entity name, operator, and a space to the string builder.
    sb.Append($"[t_{identifier.EntityName}].[{identifier.Value}] {ConvertOperator(op.Value)} ");

    // If the operator is 'in', handle the list of acceptable values.
    if (op.Value == "in")
    {
      sb.Append("("); // Start the list of values.

      // Loop through the tokens until a closing parenthesis is found or there are no more tokens.
      while (_position < _tokens.Count && FilterTokenType.Value.HasFlag(_tokens[_position].Type))
      {
        // Append the parameter name for each value.
        sb.Append($"@{_tokens[_position].ParameterName}");
        _position++;

        // If there are more tokens and they are not a closing parenthesis, append a comma and a space.
        if (_position < _tokens.Count && _tokens[_position].Type != FilterTokenType.CloseParen)
        {
          sb.Append(", ");
        }
      }

      sb.Append(")"); // End the list of values.
    } else
    {
      // If the operator is not 'in', handle a single value.
      if (FilterTokenType.Value.HasFlag(value.Type))
      {
        // If the value is a token, append the parameter name.
        sb.Append($"@{value.ParameterName}");
      } else
      {
        // If the value is not a token, append the value itself.
        sb.Append(value.Value);
      }
    }
  }

  private string ConvertOperator(string op)
  {
    // Convert the operator string to the corresponding SQL operator.
    // The switch statement handles different operator cases.
    // If the operator is not recognized, an ArgumentException is thrown.
    switch (op)
    {
      case "eq":
        return "=";
      case "ne":
        return "!=";
      case "gt":
        return ">";
      case "ge":
        return ">=";
      case "lt":
        return "<";
      case "le":
        return "<=";
      case "in":
        return "IN";
      case "ct":
      case "sw":
        return "LIKE";
      default:
        throw new ArgumentException("Unknown operator");
    }
  }
}
