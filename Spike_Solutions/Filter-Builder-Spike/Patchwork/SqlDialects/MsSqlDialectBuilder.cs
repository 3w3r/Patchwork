using System.Collections.Specialized;
using System.Data.Common;
using Azure;
using Microsoft.Data.SqlClient;
using Patchwork.DbSchema;
using Patchwork.Expansion;
using Patchwork.Fields;
using Patchwork.Filters;
using Patchwork.Paging;
using Patchwork.Sort;

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

    public override string BuildGetListSql(string schemaName, string entityName
    , string fields = ""
    , string filter = ""
    , string sort = ""
    , int limit = 0
    , int offset = 0)
    {
      if(string.IsNullOrEmpty(schemaName))
        throw new ArgumentException("Schema name is required.", nameof(schemaName));
      if (string.IsNullOrEmpty(entityName))
        throw new ArgumentException("Entity name is required.", nameof(entityName));

      var select = BuildSelectClause(fields, entityName);
      var where = string.IsNullOrEmpty(filter) ? "" : BuildWhereClause(filter, entityName);
      var orderBy = string.IsNullOrEmpty(sort) ? "" : BuildOrderByClause(sort, entityName);
      var paging = BuildLimitOffsetClause(limit, offset);

      return $"{select} {where} {orderBy} {paging}";
    }
    public override string BuildPatchListSql(string schemaName, string entityName, JsonPatchDocument jsonPatchRequestBody) { throw new NotImplementedException(); }
    public override string BuildGetSingleSql(string schemaName, string entityName, string id
    , string fields = ""
    , string include = ""
    , DateTimeOffset? asOf = null) { throw new NotImplementedException(); }
    public override string BuildPutSingleSql(string schemaName, string entityName, string id, string jsonRequestBody) { throw new NotImplementedException(); }
    public override string BuildPatchSingleSql(string schemaName, string entityName, string id, JsonPatchDocument jsonPatchRequestBody) { throw new NotImplementedException(); }
    public override string BuildDeleteSingleSql(string schemaName, string entityName, string id) { throw new NotImplementedException(); }

    public string BuildGetListSql() { return string.Empty; }
    public string BuildPatchListSql() { return string.Empty; }

    public string BuildGetSingleSql() { return string.Empty; }
    public string BuildPutSingleSql() { return string.Empty; }
    public string BuildPatchSingleSql() { return string.Empty; }
    public string BuildDeleteSingleSql() { return string.Empty; }

    public override string BuildSelectClause(string fields, string entityName)
    {
      Entity entity = FindEntity(entityName);

      if (string.IsNullOrEmpty(fields) || fields.Contains("*"))
        return $"SELECT * FROM [{entity.SchemaName}].[{entity.Name}] AS [T_{entity.Name}]";

      List<FieldsToken> tokens = GetFieldTokens(fields, entity);
      MsSqlFieldsTokenParser parser = new MsSqlFieldsTokenParser(tokens);
      string fieldList = parser.Parse();

      return $"SELECT {fieldList} FROM [{entity.SchemaName}].[{entity.Name}] AS [T_{entity.Name}]";
    }

    public override string BuildJoinClause(string includeString, string entityName)
    {
      if (string.IsNullOrEmpty(includeString))
        throw new ArgumentException(nameof(includeString));
      if (string.IsNullOrEmpty(entityName))
        throw new ArgumentException(nameof(entityName));

      try
      {
        Entity entity = FindEntity(entityName);
        List<IncludeToken> tokens = GetIncludeTokens(includeString, entity);
        MsSqlIncludeTokenParser parser = new MsSqlIncludeTokenParser(tokens);
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
        Entity entity = FindEntity(entityName);
        List<FilterToken> tokens = GetFilterTokens(filterString, entity);
        MsSqlFilterTokenParser parser = new MsSqlFilterTokenParser(tokens);
        string result = parser.Parse();
        return $"WHERE {result}";
      }
      catch (ArgumentException ex)
      {
        throw new ArgumentException($"Invalid filter string: {ex.Message}", ex);
      }
    }

    public override string BuildOrderByClause(string sort, string entityName)
    {
      try
      {
        Entity entity = FindEntity(entityName);
        List<SortToken> tokens = GetSortTokens(sort, entity);
        MsSortTokenParser parser = new MsSortTokenParser(tokens);
        string orderby = parser.Parse();
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
        PagingToken token = GetPagingToken(limit, offset);
        MsSqlPagingParser parser = new MsSqlPagingParser(token);
        return parser.Parse();
      }
      catch (ArgumentException ex)
      {
        throw new ArgumentException($"Invalid limit or offset: {ex.Message}", ex);
      }
    }
  }
}
