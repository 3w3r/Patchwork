using System.Text;
using Patchwork.SqlStatements;

namespace Patchwork.Filters;

public abstract class FilterTokenParserBase
{
  protected List<FilterToken> _tokens;
  protected int _position;
  protected static Type? _decimal;
  protected static Type? _dateTimeOffset;

  public FilterTokenParserBase(List<FilterToken> tokens)
  {
    _tokens = tokens;
    _position = 0;
    if (_decimal == null)
      _decimal = typeof(Decimal);
    if (_dateTimeOffset == null)
      _dateTimeOffset = typeof(DateTimeOffset);
  }

  /// <summary>
  /// Parses the filter tokens and constructs a SQL WHERE clause and parameters.
  /// </summary>
  /// <returns>A FilterStatement object containing the SQL WHERE clause and parameters.</returns>
  public FilterStatement Parse()
  {
    // Initialize a StringBuilder to build the SQL WHERE clause
    StringBuilder whereClause = new StringBuilder();

    // Parse the filter tokens and construct the SQL WHERE clause
    ParseExpression(whereClause);

    // Return a new FilterStatement object containing the SQL WHERE clause and parameters
    return new FilterStatement($"WHERE {whereClause.ToString()}", GetParameters());
  }

  protected abstract void ParseExpression(StringBuilder sb);

  private Dictionary<string, object> GetParameters()
  {
    // Initialize an empty dictionary to store the parameters for the SQL query
    Dictionary<string, object> parameters = new Dictionary<string, object>();

    // Iterate through each token in the list
    for (int i = 0; i < _tokens.Count; i++)
    {
      // Check if the current token is a textual value and the previous token is an operator
      if (i > 0 && _tokens[i].Type == FilterTokenType.Textual)
      {
        if (_tokens[i - 1].Value == "sw")
        {
          // If the operator is "sw", add a parameter for the "starts with" condition
          parameters.Add(_tokens[i].ParameterName, GetStartsWithValue(_tokens[i]));
        }
        else if (_tokens[i - 1].Value == "ct")
        {
          // If the operator is "ct", add a parameter for the "contains" condition
          parameters.Add(_tokens[i].ParameterName, GetContainsValue(_tokens[i]));
        }
        else
        {
          // If the operator is neither "sw" nor "ct", add a parameter for the exact value
          parameters.Add(_tokens[i].ParameterName, _tokens[i].Value);
        }
      }
      // Check if the current token is a UUID value
      else if (i > 0 && _tokens[i].Type == FilterTokenType.UUID)
      {
        // If the token is a UUID, add a parameter for the UUID value
        parameters.Add(_tokens[i].ParameterName, Guid.Parse(_tokens[i].Value));
      }
      // Check if the current token is a numeric value
      else if (i > 0 && _tokens[i].Type == FilterTokenType.Numeric)
      {
        // If the token is a numeric value, add a parameter for the numeric value, casting it to the Decimal type
        parameters.Add(_tokens[i].ParameterName, CastParameterValue(_decimal!, _tokens[i].Value));
      }
      // Check if the current token is a DateTime value
      else if (i > 0 && _tokens[i].Type == FilterTokenType.DateTime)
      {
        // If the token is a DateTime value, add a parameter for the DateTime value, casting it to the DateTimeOffset type
        parameters.Add(_tokens[i].ParameterName, CastParameterValue(_dateTimeOffset!, _tokens[i].Value));
      }
    }

    // Return the dictionary of parameters
    return parameters;
  }

  private string GetStartsWithValue(FilterToken token)
  {
    // This method constructs a "starts with" condition for the given token value
    // It appends a "%" to the end of the token value and returns the resulting string
    return $"{token.Value}%";
  }

  private string GetContainsValue(FilterToken token)
  {
    // This method constructs a "contains" condition for the given token value
    // It surrounds the token value with "%" and returns the resulting string
    return $"%{token.Value}%";
  }

  protected object CastParameterValue(Type dataFormat, object value)
  {
    // This method casts the given value to the specified data format
    // If the data format is DateTimeOffset, it parses the value as a DateTimeOffset and returns the result
    // Otherwise, it uses Convert.ChangeType to cast the value to the specified data format and returns the result
    if (dataFormat == _dateTimeOffset)
      return DateTimeOffset.Parse(value.ToString() ?? string.Empty);
    return Convert.ChangeType(value, dataFormat);
  }
}
