using System.Text;
using Patchwork.DbSchema;

namespace Patchwork.Filters
{
  public class FilterLexer
  {
    private readonly string _input;
    private int _position;
    private int _placeholderCount;
    private readonly Entity _entity;
    private readonly DatabaseMetadata _metadata;

    public FilterLexer(string input, Entity entity, DatabaseMetadata metadata)
    {
      _input = input;
      _position = 0;
      _placeholderCount = 0;
      _entity = entity;
      _metadata = metadata;
    }

    public List<FilterToken> Tokenize()
    {
      List<FilterToken> tokens = new List<FilterToken>();
      while (_position < _input.Length)
      {
        char current = _input[_position];
        if (char.IsLetter(current))
        {
          // verify if this token is an operator
          int placeholder = _position;
          FilterToken t = ReadOperator();

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
          tokens.Add(new FilterToken(current == '(' ? FilterTokenType.OpenParen : FilterTokenType.CloseParen, _entity.Name, current.ToString(), ""));
          _position++;
        }
        else if (char.IsWhiteSpace(current))
        {
          tokens.Add(new FilterToken(FilterTokenType.Whitespace, _entity.Name, " ", ""));
          _position++;
        }
        else
        {
          tokens.Add(new FilterToken(FilterTokenType.Whitespace, _entity.Name, current.ToString(), ""));
          _position++;
        }
      }
      tokens = RemoveWhitespaceTokens(tokens);

      ValidateTokenSyntax(tokens);

      return tokens;
    }

    private void ValidateTokenSyntax(List<FilterToken> tokens)
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
      for (int i = 0; i < tokens.Count - 1; i++)
      {
        if ((tokens[i].Type == FilterTokenType.Operator || tokens[i].Type == FilterTokenType.Logical) &&
            (tokens[i + 1].Type == FilterTokenType.Operator || tokens[i + 1].Type == FilterTokenType.Logical))
        {
          throw new ArgumentException("Invalid syntax: Consecutive operators or logical operators are not allowed");
        }
      }

      // Check for value token not followed by an operator or logical operator or a paren
      bool foundInOperator = false;
      for (int i = 0; i < tokens.Count - 1; i++)
      {
        if (tokens[i].Type == FilterTokenType.Identifier && !_entity.Columns.Any(c => c.Name.Equals(tokens[i].Value, StringComparison.OrdinalIgnoreCase)))
          throw new ArgumentException($"Column '{tokens[i].Value}' does not exist on {_entity.Name}.");
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
      for (int i = 0; i < tokens.Count - 1; i++)
      {
        if (tokens[i].Type == FilterTokenType.OpenParen && tokens[i + 1].Type == FilterTokenType.Logical)
        {
          throw new ArgumentException("Invalid syntax: An open parenthesis cannot be followed by logical operator.");
        }
      }

      // Check for logical operator followed by a close parenthesis
      for (int i = 0; i < tokens.Count - 1; i++)
      {
        if (tokens[i].Type == FilterTokenType.Logical && tokens[i + 1].Type == FilterTokenType.CloseParen)
        {
          throw new ArgumentException("Invalid syntax: Logical operator cannot be followed by a close parenthesis");
        }
      }

      // Check for `in` operator not followed by an open parenthesis
      for (int i = 0; i < tokens.Count - 1; i++)
      {
        if (tokens[i].Value == "in" && tokens[i + 1].Type != FilterTokenType.OpenParen)
        {
          throw new ArgumentException("Invalid syntax: 'in' operator must be followed by an open parenthesis");
        }
      }

      // Check for unmatched parentheses
      int openParenCount = 0;
      foreach (FilterToken token in tokens)
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
      StringBuilder sb = new StringBuilder();
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
      string value = sb.ToString();
      return new FilterToken(FilterTokenType.Identifier, _entity.Name, value, "");
    }

    private FilterToken ReadStringOrDateTime()
    {
      StringBuilder sb = new StringBuilder();
      _position++; // Skip opening quote
      while (_position < _input.Length && _input[_position] != '\'')
      {
        sb.Append(_input[_position++]);
      }
      if (_position < _input.Length)
        _position++; // Skip closing quote

      string value = sb.ToString();
      if (DateTime.TryParse(value, out DateTime dt))
      {
        return new FilterToken(FilterTokenType.DateTime, _entity.Name, dt.ToUniversalTime().ToString("o"), $"V{_placeholderCount++}");
      }
      else
      {
        return new FilterToken(FilterTokenType.Textual, _entity.Name, sb.ToString(), $"V{_placeholderCount++}");
      }
    }

    private FilterToken ReadNumber()
    {
      StringBuilder sb = new StringBuilder();
      while (_position < _input.Length && char.IsDigit(_input[_position]))
      {
        sb.Append(_input[_position++]);
      }
      return new FilterToken(FilterTokenType.Numeric, _entity.Name, sb.ToString(), $"V{_placeholderCount++}");
    }

    private FilterToken ReadLogicalOperator()
    {
      StringBuilder sb = new StringBuilder();
      while (_position < _input.Length && char.IsLetter(_input[_position]))
      {
        sb.Append(_input[_position++]);
      }
      string value = sb.ToString().ToLower();
      if (value.Equals("AND", StringComparison.OrdinalIgnoreCase)
       || value.Equals("OR", StringComparison.OrdinalIgnoreCase))
      {
        return new FilterToken(FilterTokenType.Logical, _entity.Name, value, "");
      }
      return new FilterToken(FilterTokenType.Invalid, _entity.Name, value, "");
    }

    private FilterToken ReadOperator()
    {
      StringBuilder sb = new StringBuilder();
      if (_position < _input.Length && char.IsLetter(_input[_position]))
      {
        sb.Append(_input[_position]);
        sb.Append(_input[_position + 1]);
      }
      string op = sb.ToString().ToLower();
      if (op == "sw" || op == "ct" || op == "in" || op == "eq" || op == "ne" || op == "gt" || op == "ge" || op == "lt" || op == "le")
      {
        _position += 2;
        return new FilterToken(FilterTokenType.Operator, _entity.Name, op, "");
      }
      return new FilterToken(FilterTokenType.Invalid, _entity.Name, op, "");
    }
  }
}
