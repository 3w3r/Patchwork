﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Patchwork.Filters
{
  public class PostgreSqlFilterTokenParser
  {
    private List<FilterToken> _tokens;
    private int _position;

    public PostgreSqlFilterTokenParser(List<FilterToken> tokens)
    {
      _tokens = tokens;
      _position = 0;
    }

    public string Parse()
    {
      var whereClause = new StringBuilder();
      ParseExpression(whereClause);
      return whereClause.ToString();
    }

    private void ParseExpression(StringBuilder sb)
    {
      if (_position >= _tokens.Count)
        return;

      if (_tokens[_position].Type == FilterTokenType.OpenParen)
      {
        sb.Append("(");
        _position++;
        ParseExpression(sb);
        if (_position < _tokens.Count && _tokens[_position].Type == FilterTokenType.CloseParen)
        {
          sb.Append(")");
          _position++;
        }
        else
        {
          throw new ArgumentException("Unmatched parenthesis");
        }
      }
      else
      {
        ParseSimpleExpression(sb);
      }

      if (_position < _tokens.Count &&
          (_tokens[_position].Value.Equals("AND", StringComparison.OrdinalIgnoreCase)
           || _tokens[_position].Value.Equals("OR", StringComparison.OrdinalIgnoreCase)))
      {
        sb.Append(" ").Append(_tokens[_position].Value.ToUpper()).Append(" ");
        _position++;
        ParseExpression(sb);
      }
      else if (_position < _tokens.Count && _tokens[_position].Type != FilterTokenType.CloseParen)
      {
        throw new ArgumentException("Expected logical operator or closing parenthesis");
      }
    }

    private void ParseSimpleExpression(StringBuilder sb)
    {
      if (_position >= _tokens.Count)
        return;

      var identifier = _tokens[_position++];
      if (identifier.Type != FilterTokenType.Identifier)
        throw new ArgumentException("Expected identifier");

      if (_position >= _tokens.Count)
        throw new ArgumentException("Expected operator after identifier");

      var op = _tokens[_position++];
      if (op.Type != FilterTokenType.Operator)
        throw new ArgumentException("Expected operator");

      if (_position >= _tokens.Count)
        throw new ArgumentException("Expected value after operator");

      var value = _tokens[_position++];

      // Handle case where value is open paren when operator is 'in'
      if (value.Type != FilterTokenType.OpenParen && op.Type == FilterTokenType.Operator && op.Value == "in")
      {
        throw new ArgumentException("Expected open paren to begin list of acceptable values");
      }
      else if (op.Value != "in" && !FilterTokenType.Value.HasFlag(value.Type))
        throw new ArgumentException("Expected value");

      sb.Append(identifier.Value.ToLower()).Append(" ").Append(ConvertOperator(op.Value)).Append(" ");

      if (op.Value == "in")
      {
        sb.Append("(");
        while (_position < _tokens.Count && FilterTokenType.Value.HasFlag(_tokens[_position].Type))
        {
          sb.Append(_tokens[_position].Type == FilterTokenType.Numeric
              ? _tokens[_position].Value
              : $"'{_tokens[_position].Value}'");
          _position++;
          if (_position < _tokens.Count && _tokens[_position].Type != FilterTokenType.CloseParen)
          {
            sb.Append(", ");
          }
        }
        sb.Append(")");
      }
      else if (op.Value == "sw")
      {
        sb.Append("E'").Append(value.Value).Append("%'");
      }
      else if (op.Value == "ct")
      {
        sb.Append("E'%").Append(value.Value).Append("%'");
      }
      else
      {
        if (value.Type == FilterTokenType.DateTime)
        {
          sb.Append($"'{value.Value}'");
        }
        else if (value.Type == FilterTokenType.Textual)
        {
          sb.Append($"'{value.Value}'");
        }
        else
        {
          sb.Append(value.Value);
        }
      }
    }

    private string ConvertOperator(string op)
    {
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
          return "ILIKE"; // Use ILIKE for case-insensitive pattern matching
        default:
          throw new ArgumentException("Unknown operator");
      }
    }
  }

}
