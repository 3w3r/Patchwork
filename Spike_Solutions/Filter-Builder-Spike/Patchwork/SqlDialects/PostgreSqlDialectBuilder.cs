﻿using System.Collections.Specialized;
using System.Data.Common;
using Azure;
using Npgsql;
using Patchwork.DbSchema;
using Patchwork.Expansion;
using Patchwork.Fields;
using Patchwork.Filters;
using Patchwork.Paging;
using Patchwork.Sort;

namespace Patchwork.SqlDialects
{
  public class PostgreSqlDialectBuilder : SqlDialectBuilderBase
  {

    public PostgreSqlDialectBuilder(string connectionString) : base(connectionString) { }
    public PostgreSqlDialectBuilder(DatabaseMetadata metadata) : base(metadata) { }

    protected override DbConnection GetConnection()
    {
      return new NpgsqlConnection(_connectionString);
    }

    public override string BuildGetListSql(string schemaName, string entityName
    , string fields = ""
    , string filter = ""
    , string sort = ""
    , int limit = 0
    , int offset = 0) { throw new NotImplementedException(); }
    public override string BuildPatchListSql(string schemaName, string entityName, JsonPatchDocument jsonPatchRequestBody) { throw new NotImplementedException(); }
    public override string BuildGetSingleSql(string schemaName, string entityName, string id
    , string fields = ""
    , string include = ""
    , DateTimeOffset? asOf = null) { throw new NotImplementedException(); }
    public override string BuildPutSingleSql(string schemaName, string entityName, string id, string jsonRequestBody) { throw new NotImplementedException(); }
    public override string BuildPatchSingleSql(string schemaName, string entityName, string id, JsonPatchDocument jsonPatchRequestBody) { throw new NotImplementedException(); }
    public override string BuildDeleteSingleSql(string schemaName, string entityName, string id) { throw new NotImplementedException(); }

    public override string BuildSelectClause(string fields, string entityName)
    {
      Entity entity = FindEntity(entityName);

      if (string.IsNullOrEmpty(fields) || fields.Contains("*"))
        return $"SELECT * FROM {entity.SchemaName.ToLower()}.{entity.Name.ToLower()} AS t_{entity.Name.ToLower()}";

      List<FieldsToken> tokens = GetFieldTokens(fields, entity);
      PostgreSqlFieldsTokenParser parser = new PostgreSqlFieldsTokenParser(tokens);
      string fieldList = parser.Parse();

      return $"SELECT {fieldList} FROM {entity.SchemaName.ToLower()}.{entity.Name.ToLower()} AS t_{entity.Name.ToLower()}";
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
        PostgreSqlIncludeTokenParser parser = new PostgreSqlIncludeTokenParser(tokens);
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
        PostgreSqlFilterTokenParser parser = new PostgreSqlFilterTokenParser(tokens);
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
        PostgreSqlSortTokenParser parser = new PostgreSqlSortTokenParser(tokens);
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
        PostgreSqlPagingParser parser = new PostgreSqlPagingParser(token);
        return parser.Parse();
      }
      catch (ArgumentException ex)
      {
        throw new ArgumentException($"Invalid limit or offset: {ex.Message}", ex);
      }
    }
  }
}
