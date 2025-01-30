using System.ComponentModel.Design;
using System.Text;
using Patchwork.SqlStatements;

namespace Patchwork.Filters;

public abstract class FilterTokenParserBase
{
  protected List<FilterToken> _tokens;
  protected int _position;

  public FilterTokenParserBase(List<FilterToken> tokens)
  {
    _tokens = tokens;
    _position = 0;
  }

  public FilterStatement Parse()
  {
    StringBuilder whereClause = new StringBuilder();
    ParseExpression(whereClause);
    return new FilterStatement($"WHERE {whereClause.ToString()}", GetParameters());
  }

  protected abstract void ParseExpression(StringBuilder sb);

  private Dictionary<string, object> GetParameters()
  {
    Dictionary<string, object> parameters = new Dictionary<string, object>();
    for(int i =0; i < _tokens.Count; i++)
    {
      if (i > 0 && _tokens[i].Type == FilterTokenType.Textual)
      {
        if (_tokens[i - 1].Value == "sw")
          parameters.Add(_tokens[i].ParameterName, GetStartsWithValue(_tokens[i]));
        else if(_tokens[i - 1].Value == "ct")
          parameters.Add(_tokens[i].ParameterName, GetContainsValue(_tokens[i]));
        else
          parameters.Add(_tokens[i].ParameterName, _tokens[i].Value);
      }
      else if(i > 0 && _tokens[i].Type == FilterTokenType.Numeric)
          parameters.Add(_tokens[i].ParameterName, _tokens[i].Value);
      else if(i > 0 && _tokens[i].Type == FilterTokenType.DateTime)
          parameters.Add(_tokens[i].ParameterName, _tokens[i].Value);
    }
    return parameters;
  }
  private string GetStartsWithValue(FilterToken token)
  {
    return $"{token.Value}%";
  }
  private string GetContainsValue(FilterToken token)
  {
    return $"%{token.Value}%";
  }
}
