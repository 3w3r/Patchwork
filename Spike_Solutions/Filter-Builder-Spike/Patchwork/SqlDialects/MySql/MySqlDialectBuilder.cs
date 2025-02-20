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
  public MySqlDialectBuilder(string connectionString, string defaultSchema = "public") : base(connectionString, defaultSchema) { }
  public MySqlDialectBuilder(DatabaseMetadata metadata, string defaultSchema = "public") : base(metadata, defaultSchema) { }

  public override WriterConnection GetWriterConnection()
  {
    MySqlConnection c = new MySqlConnection(_connectionString);
    c.Open();
    DbTransaction t = c.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);
    return new WriterConnection(c, t);
  }

  public override ReaderConnection GetReaderConnection()
  {
    MySqlConnection c = new MySqlConnection(_connectionString);
    c.Open();
    return new ReaderConnection(c);
  }

  internal override string BuildSelectClause(string fields, Entity entity)
  {
    string schemaPrefix = string.IsNullOrEmpty(entity.SchemaName) ? string.Empty : $"{entity.SchemaName}.";

    if (string.IsNullOrEmpty(fields) || fields.Contains("*"))
      return $"SELECT * FROM {schemaPrefix}{entity.Name} AS t_{entity.Name}";

    List<FieldsToken> tokens = GetFieldTokens(fields, entity);
    MySqlFieldsTokenParser parser = new MySqlFieldsTokenParser(tokens);
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
      MySqlIncludeTokenParser parser = new MySqlIncludeTokenParser(tokens);
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
      MySqlFilterTokenParser parser = new MySqlFilterTokenParser(tokens);
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
      MySqlSortTokenParser parser = new MySqlSortTokenParser(tokens);
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
      MySqlPagingParser parser = new MySqlPagingParser(token);
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
  internal override string BuildColumnListForInsert(Entity entity)
  {
    IEnumerable<string> list = entity.Columns
                     .Where(x => !x.IsComputed && !x.IsAutoNumber)
                     .OrderBy(x => x.IsPrimaryKey)
                     .ThenBy(x => x.Name)
                     .Select(x => $"{x.Name}");
    return $"({string.Join(", ", list)})";
  }
  internal override string BuildParameterListForInsert(Entity entity)
  {
    IEnumerable<string> list = entity.Columns
                     .Where(x => !x.IsComputed && !x.IsAutoNumber)
                     .OrderBy(x => x.IsPrimaryKey)
                     .ThenBy(x => x.Name)
                     .Select(x => $"@{x.Name}");
    return $"VALUES ({string.Join(", ", list)})";
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
  "INSERT INTO patchwork.patchwork_event_log (event_date, domain, entity, id, patch) " +
  "VALUES (UTC_TIMESTAMP(), @schemaname, @entityname, @id, @patch)";
}
