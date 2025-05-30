﻿using System.Data.Common;
using System.Text;
using Microsoft.Data.Sqlite;
using Patchwork.DbSchema;
using Patchwork.Expansion;
using Patchwork.Fields;
using Patchwork.Filters;
using Patchwork.Paging;
using Patchwork.Sort;
using Patchwork.SqlStatements;

namespace Patchwork.SqlDialects.Sqlite;

public class SqliteDialectBuilder : SqlDialectBuilderBase
{
  public SqliteDialectBuilder(string connectionString) : base(connectionString, "") { }
  public SqliteDialectBuilder(DatabaseMetadata metadata) : base(metadata, "") { }

  public override WriterConnection GetWriterConnection()
  {
    SqliteConnection c = new SqliteConnection(_connectionString);
    c.Open();
    DbTransaction t = c.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);
    return new WriterConnection(c, t);
  }

  public override ReaderConnection GetReaderConnection()
  {
    SqliteConnection c = new SqliteConnection(_connectionString);
    c.Open();
    return new ReaderConnection(c);
  }

  internal override SelectEventLogStatement GetSelectEventLog(Entity entity, string id, DateTimeOffset asOf)
  {
    Dictionary<string, object> parameters = new Dictionary<string, object> 
    {
      { "schema_name", entity.SchemaName },
      { "table_name", entity.Name},
      { "entity_id", entity.PrimaryKey?.Name ?? "id"},
      { "as_of", asOf},
    };
    return new SelectEventLogStatement(
      "SELECT " +
      "pk as \"Pk\", " +
      "event_date as \"EventDate\", " +
      "http_method as \"HttpMethod\", " +
      "domain as \"Domain\", " +
      "entity as \"Entity\", " +
      "id as \"Id\", " +
      "status as \"Status\", " +
      "patch as \"Patch\" " +
      "FROM patchwork.patchwork_event_log " +
      "WHERE domain = @schema_name " +
      "AND entity = @table_name " +
      "AND id = @entity_id " +
      "AND event_date <= @as_of " +
      "ORDER BY event_date ",
      parameters);
  }

  internal override string BuildSelectClause(string fields, Entity entity)
  {
    string schemaPrefix = string.IsNullOrEmpty(entity.SchemaName) ? string.Empty : $"{entity.SchemaName}.";

    if (string.IsNullOrEmpty(fields) || fields.Contains("*"))
      return $"SELECT * FROM {schemaPrefix}{entity.Name} AS t_{entity.Name}";

    List<FieldsToken> tokens = GetFieldTokens(fields, entity);
    SqliteFieldsTokenParser parser = new SqliteFieldsTokenParser(tokens);
    string fieldList = parser.Parse();

    return $"SELECT {fieldList} FROM {schemaPrefix}{entity.Name} AS t_{entity.Name}";
  }
  internal override string BuildCountClause(Entity entity)
  {
    string schemaPrefix = string.IsNullOrEmpty(entity.SchemaName) ? string.Empty : $"{entity.SchemaName}.";
    return $"SELECT COUNT(*) FROM {schemaPrefix}{entity.Name} AS t_{entity.Name}";
  }
  internal override string BuildJoinClause(string includeString, Entity entity)
  {
    if (string.IsNullOrEmpty(includeString))
      return string.Empty;

    try
    {
      List<IncludeToken> tokens = GetIncludeTokens(includeString, entity);
      SqliteIncludeTokenParser parser = new SqliteIncludeTokenParser(tokens);
      return parser.Parse();
    }
    catch (Exception ex)
    {
      throw new ArgumentException($"Invalid include string: {ex.Message}", ex);
    }
  }
  internal override FilterStatement BuildWhereClause(string filterString, Entity entity)
  {
    try
    {
      List<FilterToken> tokens = GetFilterTokens(filterString, entity);
      SqliteFilterTokenParser parser = new SqliteFilterTokenParser(tokens);
      return parser.Parse();
    }
    catch (ArgumentException ex)
    {
      throw new ArgumentException($"Invalid filter string: {ex.Message}", ex);
    }
  }
  internal override string BuildWherePkForGetClause(Entity entity)
  {
    return $"WHERE t_{entity.Name}.{entity.PrimaryKey!.Name} = @id";
  }
  internal override string BuildOrderByClause(string sort, Entity entity)
  {
    try
    {
      List<SortToken> tokens = GetSortTokens(sort, entity);
      SqliteSortTokenParser parser = new SqliteSortTokenParser(tokens);
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
      SqlitePagingParser parser = new SqlitePagingParser(token);
      return parser.Parse();
    }
    catch (ArgumentException ex)
    {
      throw new ArgumentException($"Invalid limit or offset: {ex.Message}", ex);
    }
  }

  internal override string BuildInsertClause(Entity entity)
  {
    string schema = string.IsNullOrEmpty(entity.SchemaName) ? "" : $"{entity.SchemaName}.";
    return $"INSERT INTO {schema}{entity.Name} ";
  }
  internal override string BuildColumnListForInsert(Entity entity, Dictionary<string, object> parameters)
  {
    IEnumerable<string> list = entity.Columns
                                     .Where(x => !x.IsComputed)
                                     .Where(x => !x.IsAutoNumber)
                                     .Where(x => parameters.Keys.Contains(x.Name))
                                     .OrderBy(x => x.IsPrimaryKey)
                                     .ThenBy(x => x.Name)
                                     .Select(x => x.Name);

    return $"({string.Join(", ", list)})";
  }
  internal override string BuildParameterListForInsert(Entity entity, Dictionary<string, object> parameters)
  {
    IEnumerable<string> list = entity.Columns
                                     .Where(x => !x.IsComputed)
                                     .Where(x => !x.IsAutoNumber)
                                     .Where(x => parameters.Keys.Contains(x.Name))
                                     .OrderBy(x => x.IsPrimaryKey)
                                     .ThenBy(x => x.Name)
                                     .Select(x => $"@{x.Name}");

    return $"VALUES ({string.Join(", ", list)}) RETURNING * ";
  }

  internal override string BuildUpdateClause(Entity entity)
  {
    string schema = string.IsNullOrEmpty(entity.SchemaName) ? "" : $"{entity.SchemaName}.";
    return $"UPDATE {schema}{entity.Name} ";
  }
  internal override string BuildSetClause(Dictionary<string, object> parameters, Entity entity)
  {
    StringBuilder sb = new StringBuilder();
    foreach (KeyValuePair<string, object> parameter in parameters)
    {
      Column? col = entity.Columns.FirstOrDefault(c => c.Name.Equals(parameter.Key, StringComparison.OrdinalIgnoreCase));
      if (col == null || col.IsPrimaryKey || col.IsAutoNumber || col.IsComputed)
        continue;
      sb.Append($"\n  {col.Name} = @{parameter.Key},");
    }
    return $"SET {sb.ToString().TrimEnd(',')}\n";
  }
  internal override string BuildWherePkForUpdateClause(Entity entity)
  {
    return $"WHERE {entity.PrimaryKey!.Name} = @id ";
  }

  internal override string BuildDeleteClause(Entity entity)
  {
    string schema = string.IsNullOrEmpty(entity.SchemaName) ? "" : $"{entity.SchemaName}.";
    return $"DELETE FROM {schema}{entity.Name} ";
  }
  internal override string BuildWherePkForDeleteClause(Entity entity) => BuildWherePkForUpdateClause(entity);

  protected override string GetInsertPatchTemplate() =>
    "INSERT INTO patchwork_event_log (event_date, http_method, domain, entity, id, status, patch) " +
    "VALUES (CURRENT_TIMESTAMP, @httpmethod, @schemaname, @entityname, @id, @status, @patch)";
}
