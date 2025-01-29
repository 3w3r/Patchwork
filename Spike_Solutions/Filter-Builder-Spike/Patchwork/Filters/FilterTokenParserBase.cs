using System.Text;
using Patchwork.SqlStatements;

namespace Patchwork.Filters
{
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
      foreach (FilterToken? token in _tokens.Where(t => t.ParameterName.Length > 1))
      {
        parameters.Add(token.ParameterName, token.Value);
      }
      return parameters;
    }
  }
}
