using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Patchwork.Filters
{
  public class Lexer
  {
    private string _input;
    private int _position;

    public Lexer(string input)
    {
      _input = input;
      _position = 0;
    }

    public List<Token> Tokenize()
    {
      var tokens = new List<Token>();
      while (_position < _input.Length)
      {
        var current = _input[_position];
        if (char.IsLetter(current))
        {
          // verify if this token is an operator
          var placeholder = _position;
          var t = ReadOperator();

          // if it is not an operator, then it must be an identifier
          if (t.Type == TokenType.Invalid)
          {
            _position = placeholder;
            t = ReadLogicalOperator();
          }

          // verify if this token is a logical operator
          if (t.Type == TokenType.Invalid)
          {
            _position = placeholder;
            t = ReadIdentifier();
          }

          // Add this token to the list
          tokens.Add(t);
        }
        else if (char.IsDigit(current))
        {
          tokens.Add(ReadNumber());
        }
        else if (current == '\'')
        {
          tokens.Add(ReadStringOrDateTime());
        }
        else if (current == '(' || current == ')')
        {
          tokens.Add(new Token { Type = current == '(' ? TokenType.OpenParen : TokenType.CloseParen, Value = current.ToString() });
          _position++;
        }
        else if (char.IsWhiteSpace(current))
        {
          tokens.Add(new Token { Type = TokenType.Whitespace, Value = " " });
          _position++;
        }
        else
        {
          tokens.Add(new Token { Type = TokenType.Whitespace, Value = current.ToString() });
          _position++;
        }
      }
      tokens = RemoveWhitespaceTokens(tokens);

      ValidateTokenSyntax(tokens);

      return tokens;
    }

    private static void ValidateTokenSyntax(List<Token> tokens)
    {

      // Check if the token list starts with an operator or logical operator
      if (tokens.Count > 0 && (tokens[0].Type == TokenType.Operator || tokens[0].Type == TokenType.Logical))
      {
        throw new ArgumentException("Invalid syntax: Token list cannot start with an operator or logical operator");
      }

      // Check if the token list ends with an operator or logical operator
      if (tokens.Count > 0 && (tokens[tokens.Count - 1].Type == TokenType.Operator || tokens[tokens.Count - 1].Type == TokenType.Logical))
      {
        throw new ArgumentException("Invalid syntax: Token list cannot end with an operator or logical operator");
      }

      // Check for consecutive operators or logical operators
      for (var i = 0; i < tokens.Count - 1; i++)
      {
        if ((tokens[i].Type == TokenType.Operator || tokens[i].Type == TokenType.Logical) &&
            (tokens[i + 1].Type == TokenType.Operator || tokens[i + 1].Type == TokenType.Logical))
        {
          throw new ArgumentException("Invalid syntax: Consecutive operators or logical operators are not allowed");
        }
      }

      // Check for value token not followed by an operator or logical operator or a paren
      var foundInOperator = false;
      for (var i = 0; i < tokens.Count - 1; i++)
      {
        if (tokens[i].Type == TokenType.Operator && tokens[i].Value == "in")
        {
          if (tokens[i + 1].Type != TokenType.OpenParen)
          {
            throw new ArgumentException("Invalid syntax: The 'in' operator must be followed by an open parent to start a list of values.");
          }
          foundInOperator = true;
        }
        if (tokens[i].Type == TokenType.CloseParen)
        {
          foundInOperator = false;
        }
        if (foundInOperator && tokens[i].Type == TokenType.Numeric)
        {
          if (tokens[i + 1].Type != TokenType.Numeric && tokens[i + 1].Type != TokenType.CloseParen)
          {
            throw new ArgumentException("Invalid syntax: List of values after the IN operator must all be numeric.");
          }
        }
        if (foundInOperator && tokens[i].Type == TokenType.Textual)
        {
          if (tokens[i + 1].Type != TokenType.Textual && tokens[i + 1].Type != TokenType.CloseParen)
          {
            throw new ArgumentException("Invalid syntax: List of values after the IN operator must all be textual.");
          }
        }
        if (!foundInOperator
          && (TokenType.Value.HasFlag(tokens[i].Type))
          && tokens[i + 1].Type != TokenType.Operator
          && tokens[i + 1].Type != TokenType.Logical
          && tokens[i + 1].Type != TokenType.CloseParen)
        {
          throw new ArgumentException("Invalid syntax: Value token must be followed by an operator or logical operator or a paren");
        }
      }

      // Check for logical operator followed by an open parenthesis
      for (var i = 0; i < tokens.Count - 1; i++)
      {
        if (tokens[i].Type == TokenType.OpenParen && tokens[i + 1].Type == TokenType.Logical)
        {
          throw new ArgumentException("Invalid syntax: An open parenthesis cannot be followed by logical operator.");
        }
      }

      // Check for logical operator followed by a close parenthesis
      for (var i = 0; i < tokens.Count - 1; i++)
      {
        if (tokens[i].Type == TokenType.Logical && tokens[i + 1].Type == TokenType.CloseParen)
        {
          throw new ArgumentException("Invalid syntax: Logical operator cannot be followed by a close parenthesis");
        }
      }

      // Check for `in` operator not followed by an open parenthesis
      for (var i = 0; i < tokens.Count - 1; i++)
      {
        if (tokens[i].Value == "in" && tokens[i + 1].Type != TokenType.OpenParen)
        {
          throw new ArgumentException("Invalid syntax: 'in' operator must be followed by an open parenthesis");
        }
      }

      // Check for unmatched parentheses
      var openParenCount = 0;
      foreach (var token in tokens)
      {
        if (token.Type == TokenType.OpenParen)
        {
          openParenCount++;
        }
        else if (token.Type == TokenType.CloseParen)
        {
          openParenCount--;
          if (openParenCount < 0)
          {
            throw new ArgumentException("Invalid syntax: Unmatched closing parenthesis");
          }
        }
      }
      if (openParenCount > 0)
      {
        throw new ArgumentException("Invalid syntax: Unmatched opening parenthesis");
      }
    }

    private static List<Token> RemoveWhitespaceTokens(List<Token> tokens)
    {
      return tokens.Where(t => t.Type != TokenType.Whitespace)
                   .ToList(); // Remove whitespace tokens
    }

    private Token ReadIdentifier()
    {
      var sb = new StringBuilder();
      while (
        _position < _input.Length
        && (char.IsLetterOrDigit(_input[_position])
           || _input[_position] == '_'
           || _input[_position] == '.'
           )
        )
      {
        sb.Append(_input[_position++]);
      }
      var value = sb.ToString();
      return new Token { Type = TokenType.Identifier, Value = value };
    }

    private Token ReadStringOrDateTime()
    {
      var sb = new StringBuilder();
      _position++; // Skip opening quote
      while (_position < _input.Length && _input[_position] != '\'')
      {
        sb.Append(_input[_position++]);
      }
      if (_position < _input.Length)
        _position++; // Skip closing quote

      var value = sb.ToString();
      if (DateTime.TryParse(value, out var dt))
      {
        return new Token { Type = TokenType.DateTime, Value = dt.ToUniversalTime().ToString("o") };
      }
      else
      {
        return new Token { Type = TokenType.Textual, Value = sb.ToString() };
      }
    }

    private Token ReadNumber()
    {
      var sb = new StringBuilder();
      while (_position < _input.Length && char.IsDigit(_input[_position]))
      {
        sb.Append(_input[_position++]);
      }
      return new Token { Type = TokenType.Numeric, Value = sb.ToString() };
    }

    private Token ReadLogicalOperator()
    {
      var sb = new StringBuilder();
      while (_position < _input.Length && char.IsLetter(_input[_position]))
      {
        sb.Append(_input[_position++]);
      }
      var value = sb.ToString().ToLower();
      if (value.Equals("AND", StringComparison.OrdinalIgnoreCase)
       || value.Equals("OR", StringComparison.OrdinalIgnoreCase))
      {
        return new Token { Type = TokenType.Logical, Value = value };
      }
      return new Token { Type = TokenType.Invalid, Value = value };
    }

    private Token ReadOperator()
    {
      var sb = new StringBuilder();
      if (_position < _input.Length && char.IsLetter(_input[_position]))
      {
        sb.Append(_input[_position]);
        sb.Append(_input[_position + 1]);
      }
      var op = sb.ToString().ToLower();
      if (op == "sw" || op == "ct" || op == "in" || op == "eq" || op == "ne" || op == "gt" || op == "ge" || op == "lt" || op == "le")
      {
        _position += 2;
        return new Token { Type = TokenType.Operator, Value = op };
      }
      return new Token { Type = TokenType.Invalid, Value = op };
    }
  }
}
