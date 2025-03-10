using System.Text;
using Patchwork.DbSchema;

namespace Patchwork.Filters;

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

  /// <summary>
  /// Tokenizes the input filter string into a list of FilterToken objects.
  /// </summary>
  /// <returns>A list of FilterToken objects representing the tokens in the input filter string.</returns>
  public List<FilterToken> Tokenize()
  {
    // Initialize a list to store the tokens
    List<FilterToken> tokens = new List<FilterToken>();

    // Iterate through each character in the input string
    while (_position < _input.Length)
    {
      char current = _input[_position];

      // Check if the current character is a letter
      if (char.IsLetter(current))
      {
        // Save the current position to restore if the token is not an operator or logical operator
        int placeholder = _position;

        // Try to read an operator token
        FilterToken t = ReadOperator();

        // If the token is not an operator, then it must be an identifier
        if (t.Type == FilterTokenType.Invalid)
        {
          _position = placeholder;
          t = ReadLogicalOperator();
        }

        // If the token is still not an operator or logical operator, then it must be an identifier
        if (t.Type == FilterTokenType.Invalid)
        {
          _position = placeholder;
          t = ReadIdentifier();
        }

        // Add the token to the list
        tokens.Add(t);
      }
      // Check if the current character is a digit
      else if (char.IsDigit(current))
      {
        // Read a numeric token
        tokens.Add(ReadNumber());
      }
      // Check if the current character is a single quote (')
      else if (current == '\'')
      {
        // Read a string or datetime token
        tokens.Add(ReadStringOrDateTime());
      }
      // Check if the current character is an open or close parenthesis
      else if (current == '(' || current == ')')
      {
        // Add an open or close parenthesis token to the list
        tokens.Add(new FilterToken(current == '(' ? FilterTokenType.OpenParen : FilterTokenType.CloseParen, _entity.Name, current.ToString(), ""));
        _position++;
      }
      // Check if the current character is a whitespace
      else if (char.IsWhiteSpace(current))
      {
        // Add a whitespace token to the list
        tokens.Add(new FilterToken(FilterTokenType.Whitespace, _entity.Name, " ", ""));
        _position++;
      }
      else
      {
        // Add a whitespace token to the list (for unexpected characters)
        tokens.Add(new FilterToken(FilterTokenType.Whitespace, _entity.Name, current.ToString(), ""));
        _position++;
      }
    }

    // Remove whitespace tokens from the list
    tokens = RemoveWhitespaceTokens(tokens);

    // Validate the syntax of the tokens
    ValidateTokenSyntax(tokens);

    // Return the list of tokens
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
    // Initialize a StringBuilder to build the identifier
    StringBuilder sb = new StringBuilder();

    // Loop through each character in the input string until the end or until a non-identifier character is encountered
    while (
      _position < _input.Length
      && (char.IsLetterOrDigit(_input[_position])
         || _input[_position] == '_'
         || _input[_position] == '.'
         )
      )
    {
      // Append the current character to the StringBuilder
      sb.Append(_input[_position++]);
    }

    // Convert the StringBuilder to a string to get the identifier value
    string value = sb.ToString();

    // Find the column in the entity's schema that matches the identifier value (case-insensitive)
    Column? col = _entity.Columns.FirstOrDefault(c => c.Name.Equals(value, StringComparison.OrdinalIgnoreCase));

    // If the column is not found, throw an ArgumentException with a descriptive error message
    if (col == null)
      throw new ArgumentException($"Filter parameter '{value}' not found in data schema.");

    // Return a new FilterToken with the type set to Identifier, the entity name, the column name, and an empty
    // placeholder value
    return new FilterToken(FilterTokenType.Identifier, _entity.Name, col.Name, "");
  }

  private FilterToken ReadStringOrDateTime()
  {
    // Initialize a StringBuilder to build the string value
    StringBuilder sb = new StringBuilder();

    // Skip the opening quote character
    _position++;

    // Loop through each character in the input string until the closing quote character is encountered
    while (_position < _input.Length && _input[_position] != '\'')
    {
      // Append the current character to the StringBuilder
      sb.Append(_input[_position++]);
    }

    // Skip the closing quote character
    if (_position < _input.Length)
      _position++;

    // Convert the StringBuilder to a string to get the string value
    string value = sb.ToString();

    // Try to parse the string value as a DateTime
    if (DateTime.TryParse(value, out DateTime dt))
    {
      // If successful, return a new FilterToken with the type set to DateTime, the entity name, the universal time
      // string, and an incremented placeholder value
      return new FilterToken(FilterTokenType.DateTime, _entity.Name, dt.ToUniversalTime().ToString("o"), $"V{_placeholderCount++}");
    }
    // Try to parse the string value as a Guid
    else if (Guid.TryParse(value, out Guid uuid))
    {
      // If successful, return a new FilterToken with the type set to UUID, the entity name, the string value, and an
      // incremented placeholder value
      return new FilterToken(FilterTokenType.UUID, _entity.Name, value, $"V{_placeholderCount++}");
    }
    else
    {
      // If neither a DateTime nor a Guid can be parsed, return a new FilterToken with the type set to Textual, the
      // entity name, the string value, and an incremented placeholder value
      return new FilterToken(FilterTokenType.Textual, _entity.Name, value, $"V{_placeholderCount++}");
    }
  }

  private FilterToken ReadNumber()
  {
    // Initialize a StringBuilder to build the numeric value
    StringBuilder sb = new StringBuilder();

    // Loop through each character in the input string until a non-digit character is encountered
    while (_position < _input.Length && char.IsDigit(_input[_position]))
    {
      // Append the current character to the StringBuilder
      sb.Append(_input[_position++]);
    }

    // Convert the StringBuilder to a string to get the numeric value
    string value = sb.ToString();

    // Return a new FilterToken with the type set to Numeric, the entity name, the numeric value, and an incremented
    // placeholder value
    return new FilterToken(FilterTokenType.Numeric, _entity.Name, value, $"V{_placeholderCount++}");
  }

  private FilterToken ReadLogicalOperator()
  {
    // Initialize a StringBuilder to build the logical operator value
    StringBuilder sb = new StringBuilder();

    // Loop through each character in the input string until a non-letter character is encountered
    while (_position < _input.Length && char.IsLetter(_input[_position]))
    {
      // Append the current character to the StringBuilder
      sb.Append(_input[_position++]);
    }

    // Convert the StringBuilder to a string to get the logical operator value
    string value = sb.ToString().ToLower();

    // Check if the logical operator value is either "AND" or "OR" (case-insensitive)
    if (value.Equals("AND", StringComparison.OrdinalIgnoreCase)
     || value.Equals("OR", StringComparison.OrdinalIgnoreCase))
    {
      // If the logical operator value is valid, return a new FilterToken with the type set to Logical, the entity name,
      // the logical operator value, and an empty placeholder value
      return new FilterToken(FilterTokenType.Logical, _entity.Name, value, "");
    }

    // If the logical operator value is not valid, return a new FilterToken with the type set to Invalid, the entity
    // name, the logical operator value, and an empty placeholder value
    return new FilterToken(FilterTokenType.Invalid, _entity.Name, value, "");
  }

  private FilterToken ReadOperator()
  {
    // Initialize a StringBuilder to build the operator value
    StringBuilder sb = new StringBuilder();

    // Check if there are at least two characters left in the input string and if the first character is a letter
    if (_position < _input.Length && char.IsLetter(_input[_position]))
    {
      // Append the first character to the StringBuilder
      sb.Append(_input[_position]);

      // Check if there is at least one more character left in the input string and if the second character is a letter
      if (_position + 1 < _input.Length && char.IsLetter(_input[_position + 1]))
      {
        // Append the second character to the StringBuilder
        sb.Append(_input[_position + 1]);
      }
    }

    // Convert the StringBuilder to a string to get the operator value
    string op = sb.ToString().ToLower();

    // Check if the operator value is one of the valid operator values (case-insensitive)
    if (op == "sw" || op == "ct" || op == "in" || op == "eq" || op == "ne" || op == "gt" || op == "ge" || op == "lt" || op == "le")
    {
      // If the operator value is valid, skip the two characters that have been read and return a new FilterToken with
      // the type set to Operator, the entity name, the operator value, and an empty placeholder value
      _position += 2;
      return new FilterToken(FilterTokenType.Operator, _entity.Name, op, "");
    }

    // If the operator value is not valid, return a new FilterToken with the type set to Invalid, the entity name, the
    // operator value, and an empty placeholder value
    return new FilterToken(FilterTokenType.Invalid, _entity.Name, op, "");
  }
}
