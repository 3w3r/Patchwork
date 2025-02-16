﻿using System.Data.Common;
using Azure;
using Microsoft.Data.SqlClient;
using Patchwork.DbSchema;
using Patchwork.Expansion;
using Patchwork.Fields;
using Patchwork.Filters;
using Patchwork.Paging;
using Patchwork.Sort;
using Patchwork.SqlStatements;

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

    public override string BuildPatchListSql(string schemaName, string entityName, JsonPatchDocument jsonPatchRequestBody) { throw new NotImplementedException(); }
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

    public override FilterStatement BuildWhereClause(string filterString, string entityName)
    {
      try
      {
        Entity entity = FindEntity(entityName);
        List<FilterToken> tokens = GetFilterTokens(filterString, entity);
        MsSqlFilterTokenParser parser = new MsSqlFilterTokenParser(tokens);
        FilterStatement result = parser.Parse();

        return result;
      }
      catch (ArgumentException ex)
      {
        throw new ArgumentException($"Invalid filter string: {ex.Message}", ex);
      }
    }

    public override string BuildGetByPkClause(string entityName)
    {
      Entity entity = FindEntity(entityName);
      return $"WHERE [T_{entity.SchemaName}].[{entity.Name}].[{entity.PrimaryKey.Name}] = @Id";
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
