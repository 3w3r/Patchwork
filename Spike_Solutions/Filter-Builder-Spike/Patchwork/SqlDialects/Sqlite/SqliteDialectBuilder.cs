using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Patchwork.DbSchema;
using Patchwork.Expansion;
using Patchwork.Fields;
using Patchwork.Filters;
using Patchwork.Paging;
using Patchwork.Sort;
using Patchwork.SqlDialects.MySql;
using Patchwork.SqlStatements;

namespace Patchwork.SqlDialects.Sqlite;


public class SqliteDialectBuilder : SqlDialectBuilderBase
{
  public SqliteDialectBuilder(string connectionString) : base(connectionString) { }
  public SqliteDialectBuilder(DatabaseMetadata metadata) : base(metadata) { }

  protected override DbConnection GetConnection()
  {
    return new SqliteConnection(_connectionString);
  }

  internal override string BuildSelectClause(string fields, string entityName)
  {
    Entity entity = FindEntity(entityName);
    var schemaPrefix = string.IsNullOrEmpty(entity.SchemaName) ? string.Empty : $"{entity.SchemaName}.";

    if (string.IsNullOrEmpty(fields) || fields.Contains("*"))
      return $"SELECT * FROM {schemaPrefix}{entity.Name} AS t_{entity.Name}";

    List<FieldsToken> tokens = GetFieldTokens(fields, entity);
    var parser = new SqliteFieldsTokenParser(tokens);
    string fieldList = parser.Parse();

    return $"SELECT {fieldList} FROM {schemaPrefix}{entity.Name} AS t_{entity.Name}";
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
      var parser = new SqliteIncludeTokenParser(tokens);
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
      var parser = new SqliteFilterTokenParser(tokens);
      return parser.Parse();
    }
    catch (ArgumentException ex)
    {
      throw new ArgumentException($"Invalid filter string: {ex.Message}", ex);
    }
  }
  internal override string BuildGetByPkClause(string entityName)
  {
    Entity entity = FindEntity(entityName);
    return $"WHERE t_{entity.Name}.{entity.PrimaryKey!.Name} = @id";
  }
  internal override string BuildOrderByClause(string sort, string entityName)
  {
    try
    {
      Entity entity = FindEntity(entityName);
      List<SortToken> tokens = GetSortTokens(sort, entity);
      var parser = new SqliteSortTokenParser(tokens);
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
      var parser = new SqlitePagingParser(token);
      return parser.Parse();
    }
    catch (ArgumentException ex)
    {
      throw new ArgumentException($"Invalid limit or offset: {ex.Message}", ex);
    }
  }
}
