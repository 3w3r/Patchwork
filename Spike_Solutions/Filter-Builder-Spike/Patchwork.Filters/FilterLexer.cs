using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Patchwork.Filters
{
  public class FilterLexer
  {
    private string _input;
    private int _position;

    public FilterLexer(string input)
    {
      _input = input;
      _position = 0;
    }

    public List<FilterToken> Tokenize()
    {
      var tokens = new List<FilterToken>();
      while (_position < _input.Length)
      {
        var current = _input[_position];
        if (char.IsLetter(current))
        {
          // verify if this token is an operator
          var placeholder = _position;
          var t = ReadOperator();

          // if it is not an operator, then it must be an identifier
          if (t.Type == FilterTokenType.Invalid)
          {
            _position = placeholder;
            t = ReadLogicalOperator();
          }

          // verify if this token is a logical operator
          if (t.Type == FilterTokenType.Invalid)
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
          tokens.Add(new FilterToken(current == '(' ? FilterTokenType.OpenParen : FilterTokenType.CloseParen, current.ToString()));
          _position++;
        }
        else if (char.IsWhiteSpace(current))
        {
          tokens.Add(new FilterToken(FilterTokenType.Whitespace, " "));
          _position++;
        }
        else
        {
          tokens.Add(new FilterToken(FilterTokenType.Whitespace, current.ToString()));
          _position++;
        }
      }
      tokens = RemoveWhitespaceTokens(tokens);

      ValidateTokenSyntax(tokens);

      return tokens;
    }

    private static void ValidateTokenSyntax(List<FilterToken> tokens)
    {

      // Check if the token list starts with an operator or logical operator
      if (tokens.Count > 0 && (tokens[0].Type == FilterTokenType.Operator || tokens[0].Type == FilterTokenType.Logical))
      {
        throw new ArgumentException("Invalid syntax: Token list cannot start with an operator or logical operator");
      }

      // Check if the token list ends with an operator or logical operator
      if (tokens.Count > 0 && (tokens[tokens.Count - 1].Type == FilterTokenType.Operator || tokens[tokens.Count - 1].Type == FilterTokenType.Logical))
      {
        throw new ArgumentException("Invalid syntax: Token list cannot end with an operator or logical operator");
      }

      // Check for consecutive operators or logical operators
      for (var i = 0; i < tokens.Count - 1; i++)
      {
        if ((tokens[i].Type == FilterTokenType.Operator || tokens[i].Type == FilterTokenType.Logical) &&
            (tokens[i + 1].Type == FilterTokenType.Operator || tokens[i + 1].Type == FilterTokenType.Logical))
        {
          throw new ArgumentException("Invalid syntax: Consecutive operators or logical operators are not allowed");
        }
      }

      // Check for value token not followed by an operator or logical operator or a paren
      var foundInOperator = false;
      for (var i = 0; i < tokens.Count - 1; i++)
      {
        if (tokens[i].Type == FilterTokenType.Operator && tokens[i].Value == "in")
        {
          if (tokens[i + 1].Type != FilterTokenType.OpenParen)
          {
            throw new ArgumentException("Invalid syntax: The 'in' operator must be followed by an open parent to start a list of values.");
          }
          foundInOperator = true;
        }
        if (tokens[i].Type == FilterTokenType.CloseParen)
        {
          foundInOperator = false;
        }
        if (foundInOperator && tokens[i].Type == FilterTokenType.Numeric)
        {
          if (tokens[i + 1].Type != FilterTokenType.Numeric && tokens[i + 1].Type != FilterTokenType.CloseParen)
          {
            throw new ArgumentException("Invalid syntax: List of values after the IN operator must all be numeric.");
          }
        }
        if (foundInOperator && tokens[i].Type == FilterTokenType.Textual)
        {
          if (tokens[i + 1].Type != FilterTokenType.Textual && tokens[i + 1].Type != FilterTokenType.CloseParen)
          {
            throw new ArgumentException("Invalid syntax: List of values after the IN operator must all be textual.");
          }
        }
        if (!foundInOperator
          && (FilterTokenType.Value.HasFlag(tokens[i].Type))
          && tokens[i + 1].Type != FilterTokenType.Operator
          && tokens[i + 1].Type != FilterTokenType.Logical
          && tokens[i + 1].Type != FilterTokenType.CloseParen)
        {
          throw new ArgumentException("Invalid syntax: Value token must be followed by an operator or logical operator or a paren");
        }
      }

      // Check for logical operator followed by an open parenthesis
      for (var i = 0; i < tokens.Count - 1; i++)
      {
        if (tokens[i].Type == FilterTokenType.OpenParen && tokens[i + 1].Type == FilterTokenType.Logical)
        {
          throw new ArgumentException("Invalid syntax: An open parenthesis cannot be followed by logical operator.");
        }
      }

      // Check for logical operator followed by a close parenthesis
      for (var i = 0; i < tokens.Count - 1; i++)
      {
        if (tokens[i].Type == FilterTokenType.Logical && tokens[i + 1].Type == FilterTokenType.CloseParen)
        {
          throw new ArgumentException("Invalid syntax: Logical operator cannot be followed by a close parenthesis");
        }
      }

      // Check for `in` operator not followed by an open parenthesis
      for (var i = 0; i < tokens.Count - 1; i++)
      {
        if (tokens[i].Value == "in" && tokens[i + 1].Type != FilterTokenType.OpenParen)
        {
          throw new ArgumentException("Invalid syntax: 'in' operator must be followed by an open parenthesis");
        }
      }

      // Check for unmatched parentheses
      var openParenCount = 0;
      foreach (var token in tokens)
      {
        if (token.Type == FilterTokenType.OpenParen)
        {
          openParenCount++;
        }
        else if (token.Type == FilterTokenType.CloseParen)
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

    private static List<FilterToken> RemoveWhitespaceTokens(List<FilterToken> tokens)
    {
      return tokens.Where(t => t.Type != FilterTokenType.Whitespace)
                   .ToList(); // Remove whitespace tokens
    }

    private FilterToken ReadIdentifier()
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
      return new FilterToken(FilterTokenType.Identifier, value);
    }

    private FilterToken ReadStringOrDateTime()
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
        return new FilterToken(FilterTokenType.DateTime, dt.ToUniversalTime().ToString("o"));
      }
      else
      {
        return new FilterToken(FilterTokenType.Textual, sb.ToString());
      }
    }

    private FilterToken ReadNumber()
    {
      var sb = new StringBuilder();
      while (_position < _input.Length && char.IsDigit(_input[_position]))
      {
        sb.Append(_input[_position++]);
      }
      return new FilterToken(FilterTokenType.Numeric, sb.ToString());
    }

    private FilterToken ReadLogicalOperator()
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
        return new FilterToken(FilterTokenType.Logical, value);
      }
      return new FilterToken(FilterTokenType.Invalid, value);
    }

    private FilterToken ReadOperator()
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
        return new FilterToken(FilterTokenType.Operator, op);
      }
      return new FilterToken(FilterTokenType.Invalid, op);
    }
  }
}
