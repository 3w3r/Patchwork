using System;
using Patchwork.Filters;
using Patchwork.Sort;
using Patchwork.Paging;
using Patchwork.DbSchema;
using Microsoft.Data.SqlClient;
using Patchwork.Expansion;
using System.Data.Common;

namespace Patchwork.SqlDialects
{
  public class MsSqlDialectBuilder : SqlDialectBuilderBase
  {
    public MsSqlDialectBuilder(string connectionString) : base(connectionString) { }

    public MsSqlDialectBuilder(DatabaseMetadata metadata) : base(metadata) { }

    protected override DbConnection GetConnection()
    {
      return new SqlConnection(_connectionString);
    }

    public override string BuildSelectClause(string entityName)
    {
      var entity = FindEntity(entityName);
      return $"SELECT * FROM [{entity.SchemaName}].[{entity.Name}] AS [T_{entity.Name}]";
    }

    public override string BuildJoinClause(string includeString, string entityName)
    {
      if (string.IsNullOrEmpty(includeString)) throw new ArgumentException(nameof(includeString));
      if (string.IsNullOrEmpty(entityName)) throw new ArgumentException(nameof(entityName));

      try
      {
        var entity = FindEntity(entityName);
        var tokens = GetIncludeTokens(includeString, entity);
        var parser = new MsSqlIncludeTokenParser(tokens);
        return parser.Parse();
      }
      catch (Exception ex)
      {
        throw new ArgumentException($"Invalid include string: {ex.Message}", ex);
      }
    }

    public override string BuildWhereClause(string filterString, string entityName)
    {
      try
      {
        var entity = FindEntity(entityName);
        var tokens = GetFilterTokens(filterString, entity);
        var parser = new MsSqlFilterTokenParser(tokens);
        var result = parser.Parse();
        return $"WHERE {result}";
      }
      catch (ArgumentException ex)
      {
        throw new ArgumentException($"Invalid filter string: {ex.Message}", ex);
      }
    }

    public override string BuildOrderByClause(string sort, string pkName, string entityName)
    {
      try
      {
        var entity = FindEntity(entityName);
        var tokens = GetSortTokens(sort, entity);
        var parser = new MsSortTokenParser(tokens);
        var orderby = parser.Parse();
        return $"ORDER BY {orderby}";
      }
      catch (ArgumentException ex)
      {
        throw new ArgumentException($"Invalid sort string: {ex.Message}", ex);
      }
    }

    public override string BuildLimitOffsetClause(int limit, int offset)
    {
      try
      {
        var token = new PagingToken(limit, offset);
        var parser = new MsSqlPagingParser(token);
        return parser.Parse();
      }
      catch (ArgumentException ex)
      {
        throw new ArgumentException($"Invalid limit or offset: {ex.Message}", ex);
      }
    }
  }
}
