using System.Data.Common;
using System.Text;
using Npgsql;
using Patchwork.DbSchema;
using Patchwork.Expansion;
using Patchwork.Fields;
using Patchwork.Filters;
using Patchwork.Paging;
using Patchwork.Sort;
using Patchwork.SqlStatements;

namespace Patchwork.SqlDialects.PostgreSql;

public class PostgreSqlDialectBuilder : SqlDialectBuilderBase
{
  static readonly string StringType = typeof(string).Name;

  public PostgreSqlDialectBuilder(string connectionString) : base(connectionString, "public") { }
  public PostgreSqlDialectBuilder(DatabaseMetadata metadata) : base(metadata, "public") { }

  public override ActiveConnection GetConnection()
  {
    NpgsqlConnection c = new NpgsqlConnection(_connectionString);
    c.Open();
    DbTransaction t = c.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);
    return new ActiveConnection(c, t);
  }

  internal override string BuildSelectClause(string fields, Entity entity)
  {
    string schemaPrefix = string.IsNullOrEmpty(entity.SchemaName) ? string.Empty : $"{entity.SchemaName.ToLower()}.";

    if (string.IsNullOrEmpty(fields) || fields.Contains("*"))
      return $"SELECT * FROM {schemaPrefix}{entity.Name.ToLower()} AS t_{entity.Name.ToLower()}";

    List<FieldsToken> tokens = GetFieldTokens(fields, entity);
    PostgreSqlFieldsTokenParser parser = new PostgreSqlFieldsTokenParser(tokens);
    string fieldList = parser.Parse();

    return $"SELECT {fieldList} FROM {schemaPrefix}{entity.Name.ToLower()} AS t_{entity.Name.ToLower()}";
  }
  internal override string BuildCountClause(Entity entity)
  {
    string schemaPrefix = string.IsNullOrEmpty(entity.SchemaName) ? string.Empty : $"{entity.SchemaName.ToLower()}.";
    return $"SELECT COUNT(*) FROM {schemaPrefix}{entity.Name.ToLower()} AS t_{entity.Name.ToLower()}";
  }
  internal override string BuildJoinClause(string includeString, Entity entity)
  {
    if (string.IsNullOrEmpty(includeString))
      throw new ArgumentException(includeString, nameof(includeString));

    try
    {
      List<IncludeToken> tokens = GetIncludeTokens(includeString, entity);
      PostgreSqlIncludeTokenParser parser = new PostgreSqlIncludeTokenParser(tokens);
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
      PostgreSqlFilterTokenParser parser = new PostgreSqlFilterTokenParser(tokens);
      FilterStatement result = parser.Parse();

      return result;
    }
    catch (ArgumentException ex)
    {
      throw new ArgumentException($"Invalid filter string: {ex.Message}", ex);
    }
  }
  internal override string BuildWherePkForGetClause(Entity entity)
  {
    return $"WHERE t_{entity.Name.ToLower()}.{entity.PrimaryKey!.Name} = @id";
  }
  internal override string BuildOrderByClause(string sort, Entity entity)
  {
    try
    {
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
  internal override string BuildLimitOffsetClause(int limit, int offset)
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

  internal override string BuildInsertClause(Entity entity)
  {
    string schema = string.IsNullOrEmpty(entity.SchemaName) ? "" : $"{entity.SchemaName.ToLower()}.";
    return $"INSERT INTO {schema}{entity.Name.ToLower()} ";
  }
  internal override string BuildColumnListForInsert(Entity entity)
  {
    IEnumerable<string> list = entity.Columns
                     .Where(x => !x.IsComputed && !x.IsAutoNumber)
                     .OrderBy(x => x.IsPrimaryKey)
                     .ThenBy(x => x.Name)
                     .Select(x => x.Name.ToLower());
    return $"({string.Join(", ", list)})";
  }
  internal override string BuildParameterListForInsert(Entity entity)
  {
    IEnumerable<string> list = entity.Columns
                     .Where(x => !x.IsComputed && !x.IsAutoNumber)
                     .OrderBy(x => x.IsPrimaryKey)
                     .ThenBy(x => x.Name)
                     .Select(x => $"@{x.Name.ToLower()}");
    return $"VALUES ({string.Join(", ", list)}) RETURNING *";
  }


  internal override string BuildUpdateClause(Entity entity)
  {
    string schema = string.IsNullOrEmpty(entity.SchemaName) ? "" : $"{entity.SchemaName.ToLower()}.";
    return $"UPDATE {schema}{entity.Name} ";
  }
  internal override string BuildSetClause(Dictionary<string, object> parameters, Entity entity)
  {
    StringBuilder sb = new StringBuilder();
    foreach (KeyValuePair<string, object> parameter in parameters)
    {
      Column? col = entity.Columns.FirstOrDefault(c => c.Name.Equals(parameter.Key, StringComparison.OrdinalIgnoreCase));
      if (col == null || col.IsAutoNumber || col.IsComputed || col.IsPrimaryKey)
        continue;
      sb.Append($"\n  {col.Name.ToLower()} = @{parameter.Key},");
    }
    return $"SET {sb.ToString().TrimEnd(',')}\n";
  }

  internal override string BuildWherePkForUpdateClause(Entity entity)
  {
    return $"WHERE {entity.PrimaryKey!.Name.ToLower()} = @id ";
  }

  internal override string BuildDeleteClause(Entity entity)
  {
    string schema = string.IsNullOrEmpty(entity.SchemaName) ? "" : $"{entity.SchemaName.ToLower()}.";
    return $"DELETE FROM {schema}{entity.Name.ToLower()} ";
  }
  internal override string BuildWherePkForDeleteClause(Entity entity) => BuildWherePkForUpdateClause(entity);
  protected override string GetInsertPatchTemplate() =>
    "INSERT INTO patchwork.patchwork_event_log (event_date, domain, entity, id, patch) " +
    "VALUES (CURRENT_TIMESTAMP AT TIME ZONE 'UTC', @schemaname, @entityname, @id, @patch) RETURNING *";
}
