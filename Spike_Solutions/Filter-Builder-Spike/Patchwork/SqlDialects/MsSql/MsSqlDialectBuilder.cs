using System.Data.Common;
using System.Text;
using Microsoft.Data.SqlClient;
using Patchwork.DbSchema;
using Patchwork.Expansion;
using Patchwork.Fields;
using Patchwork.Filters;
using Patchwork.Paging;
using Patchwork.Sort;
using Patchwork.SqlStatements;

namespace Patchwork.SqlDialects.MsSql;

public class MsSqlDialectBuilder : SqlDialectBuilderBase
{
  public MsSqlDialectBuilder(string connectionString) : base(connectionString) { }

  public MsSqlDialectBuilder(DatabaseMetadata metadata) : base(metadata) { }

  protected override DbConnection GetConnection()
  {
    return new SqlConnection(_connectionString);
  }

  internal override string BuildSelectClause(string fields, string entityName)
  {
    Entity entity = FindEntity(entityName);
    string schemaPrefix = string.IsNullOrEmpty(entity.SchemaName) ? string.Empty : $"[{entity.SchemaName}].";

    if (string.IsNullOrEmpty(fields) || fields.Contains("*"))
      return $"SELECT * FROM {schemaPrefix}[{entity.Name}] AS [T_{entity.Name}]";

    List<FieldsToken> tokens = GetFieldTokens(fields, entity);
    MsSqlFieldsTokenParser parser = new MsSqlFieldsTokenParser(tokens);
    string fieldList = parser.Parse();

    return $"SELECT {fieldList} FROM {schemaPrefix}[{entity.Name}] AS [T_{entity.Name}]";
  }
  internal override string BuildJoinClause(string includeString, string entityName)
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
  internal override FilterStatement BuildWhereClause(string filterString, string entityName)
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
  internal override string BuildWherePkForGetClause(string entityName)
  {
    Entity entity = FindEntity(entityName);
    return $"WHERE [T_{entity.SchemaName}].[{entity.Name}].[{entity.PrimaryKey!.Name}] = @id";
  }
  internal override string BuildOrderByClause(string sort, string entityName)
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
  internal override string BuildLimitOffsetClause(int limit, int offset)
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

  internal override string BuildUpdateClause(string entityName)
  {
    Entity entity = FindEntity(entityName);
    string schema = string.IsNullOrEmpty(entity.SchemaName) ? "" : $"[{entity.SchemaName}].";
    return $"UPDATE {schema}[{entityName}] ";
  }
  internal override string BuildSetClause(Dictionary<string, object> parameters, Entity entity)
  {
    StringBuilder sb = new StringBuilder();
    foreach (KeyValuePair<string, object> parameter in parameters)
    {
      Column? col = entity.Columns.FirstOrDefault(c => c.Name.Equals(parameter.Key, StringComparison.OrdinalIgnoreCase));
      if (col == null || col.IsPrimaryKey || col.IsAutoNumber || col.IsComputed)
        continue;
      sb.Append($"\n  [{col.Name}] = @{parameter.Key},");
    }
    return $"SET {sb.ToString().TrimEnd(',')}\n";
  }
  internal override string BuildWherePkForUpdateClause(string entityName)
  {
    Entity entity = FindEntity(entityName);
    return $"WHERE [{entity.PrimaryKey!.Name}] = @id ";
  }
}
