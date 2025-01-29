using System.Data.Common;
using Azure;
using MySqlConnector;
using Patchwork.DbSchema;
using Patchwork.Expansion;
using Patchwork.Fields;
using Patchwork.Filters;
using Patchwork.Paging;
using Patchwork.Sort;
using Patchwork.SqlStatements;

namespace Patchwork.SqlDialects
{
  public class MySqlDialectBuilder : SqlDialectBuilderBase
  {
    public MySqlDialectBuilder(string connectionString) : base(connectionString) { }
    public MySqlDialectBuilder(DatabaseMetadata metadata) : base(metadata) { }

    protected override DbConnection GetConnection()
    {
      return new MySqlConnection(_connectionString);
    }

    public override string BuildPatchListSql(string schemaName, string entityName, JsonPatchDocument jsonPatchRequestBody) { throw new NotImplementedException(); }
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
        return string.Empty;
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
    public override FilterStatement BuildWhereClause(string filterString, string entityName)
    {
      try
      {
        Entity entity = FindEntity(entityName);
        List<FilterToken> tokens = GetFilterTokens(filterString, entity);
        MySqlFilterTokenParser parser = new MySqlFilterTokenParser(tokens);
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
      return $"WHERE t_{entity.Name}.{entity.PrimaryKey.Name} = @Id";
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
