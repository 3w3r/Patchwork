using System.Data.Common;
using System.Text;
using MySqlConnector;
using Patchwork.DbSchema;
using Patchwork.Expansion;
using Patchwork.Fields;
using Patchwork.Filters;
using Patchwork.Paging;
using Patchwork.Sort;
using Patchwork.SqlStatements;

namespace Patchwork.SqlDialects.MySql;

public class MySqlDialectBuilder : SqlDialectBuilderBase
{
  public MySqlDialectBuilder(string connectionString) : base(connectionString) { }
  public MySqlDialectBuilder(DatabaseMetadata metadata) : base(metadata) { }

  protected override DbConnection GetConnection()
  {
    return new MySqlConnection(_connectionString);
  }

  internal override string BuildSelectClause(string fields, string entityName)
  {
    Entity entity = FindEntity(entityName);
    var schemaPrefix = string.IsNullOrEmpty(entity.SchemaName) ? string.Empty : $"`{entity.SchemaName.ToLower()}`.";

    if (string.IsNullOrEmpty(fields) || fields.Contains("*"))
      return $"SELECT * FROM {schemaPrefix}`{entity.Name.ToLower()}` AS t_{entity.Name.ToLower()}";

    List<FieldsToken> tokens = GetFieldTokens(fields, entity);
    var parser = new MySqlFieldsTokenParser(tokens);
    string fieldList = parser.Parse();

    return $"SELECT {fieldList} FROM {schemaPrefix}`{entity.Name.ToLower()}` AS t_{entity.Name.ToLower()}";
  }
  internal override string BuildJoinClause(string includeString, string entityName)
  {
    if (string.IsNullOrEmpty(includeString))
      return string.Empty;
    if (string.IsNullOrEmpty(entityName))
      throw new ArgumentException(nameof(entityName));

    try
    {
      Entity entity = FindEntity(entityName);
      List<IncludeToken> tokens = GetIncludeTokens(includeString, entity);
      var parser = new MySqlIncludeTokenParser(tokens);
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
      MySqlFilterTokenParser parser = new MySqlFilterTokenParser(tokens);
      return parser.Parse();
    }
    catch (ArgumentException ex)
    {
      throw new ArgumentException($"Invalid filter string: {ex.Message}", ex);
    }
  }
  internal override string BuildWherePkForGetClause(string entityName)
  {
    Entity entity = FindEntity(entityName);
    return $"WHERE t_{entity.Name}.`{entity.PrimaryKey!.Name}` = @id";
  }
  internal override string BuildOrderByClause(string sort, string entityName)
  {
    try
    {
      Entity entity = FindEntity(entityName);
      List<SortToken> tokens = GetSortTokens(sort, entity);
      var parser = new MySqlSortTokenParser(tokens);
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
      var parser = new MySqlPagingParser(token);
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
    string schema = string.IsNullOrEmpty(entity.SchemaName) ? "" : $"`{entity.SchemaName}`.";
    return $"UPDATE {schema}`{entityName}` ";
  }
  internal override string BuildSetClause(Dictionary<string, object> parameters, Entity entity)
  {
    StringBuilder sb = new StringBuilder();
    foreach (KeyValuePair<string, object> parameter in parameters)
    {
      Column? col = entity.Columns.FirstOrDefault(c => c.Name.Equals(parameter.Key, StringComparison.OrdinalIgnoreCase));
      if (col == null || col.IsPrimaryKey || col.IsAutoNumber || col.IsComputed)
        continue;
      sb.Append($"\n  `{col.Name}` = @{parameter.Key},");
    }
    return $"SET {sb.ToString().TrimEnd(',')}\n";
  }
  internal override string BuildWherePkForUpdateClause(string entityName)
  {
    Entity entity = FindEntity(entityName);
    return $"WHERE `{entity.PrimaryKey!.Name}` = @id ";
  }
}
