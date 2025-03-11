using Npgsql;

using Patchwork.DbSchema;
using Patchwork.SqlStatements;

using System.Data.Common;
using System.Text;

namespace Patchwork.SqlDialects.PostgreSql;

public class PostgreSqlDialectBuilder : SqlDialectBuilderBase
{
  static readonly string StringType = typeof(string).Name;

  public PostgreSqlDialectBuilder(string connectionString) : base(connectionString, "public") { }
  public PostgreSqlDialectBuilder(DatabaseMetadata metadata) : base(metadata, "public") { }

  /// <summary>
  ///   Gets a WriterConnection object for executing SQL commands that modify data in the database.
  /// </summary>
  /// <returns>A WriterConnection object for executing SQL commands that modify data in the database.</returns>
  public override WriterConnection GetWriterConnection()
  {
    // Create a new NpgsqlConnection object using the provided connection string
    var c = new NpgsqlConnection(_connectionString);

    // Open the connection to the database
    c.Open();

    // Begin a new transaction with the specified isolation level
    DbTransaction t = c.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);

    // Return a new WriterConnection object with the opened connection and the started transaction
    return new WriterConnection(c, t);
  }

  /// <summary>
  ///   Gets a ReaderConnection object for executing SQL commands that retrieve data from the database.
  /// </summary>
  /// <returns>A ReaderConnection object for executing SQL commands that retrieve data from the database.</returns>
  public override ReaderConnection GetReaderConnection()
  {
    // Create a new NpgsqlConnection object using the provided connection string
    var c = new NpgsqlConnection(_connectionString);

    // Open the connection to the database
    c.Open();

    // Return a new ReaderConnection object with the opened connection
    return new ReaderConnection(c);
  }

  internal override string BuildSelectClause(string fields, Entity entity)
  {
    // Check if the schema name is empty, if so, use an empty string, otherwise, convert the schema name to lowercase and append a dot
    var schemaPrefix = string.IsNullOrEmpty(entity.SchemaName) ? string.Empty : $"{entity.SchemaName.ToLower()}.";

    // Check if the fields string is empty or contains "*", if so, return a SELECT statement that selects all columns from the table
    if (string.IsNullOrEmpty(fields) || fields.Contains("*"))
      return $"SELECT * FROM {schemaPrefix}{entity.Name.ToLower()} AS t_{entity.Name.ToLower()}";

    // If the fields string is not empty and does not contain "*", parse the fields string into a list of FieldTokens
    var tokens = GetFieldTokens(fields, entity);

    // Create a new instance of the PostgreSqlFieldsTokenParser class and pass the list of FieldTokens to it
    var parser = new PostgreSqlFieldsTokenParser(tokens);

    // Parse the list of FieldTokens into a field list string
    var fieldList = parser.Parse();

    // Return a SELECT statement that selects the parsed field list from the table
    return $"SELECT {fieldList} FROM {schemaPrefix}{entity.Name.ToLower()} AS t_{entity.Name.ToLower()}";
  }

  internal override SelectEventLogStatement GetSelectEventLog(Entity entity, string id, DateTimeOffset asOf)
  {
    var parameters = new Dictionary<string, object>
    {
      { "schema_name", entity.SchemaName },
      { "table_name", entity.Name},
      //{ "entity_id", entity.PrimaryKey?.Name ?? "id"},
      { "entity_id", id},
      { "as_of", asOf},
    };
    return new SelectEventLogStatement(
      "SELECT " +
      "p.pk as \"Pk\", " +
      "p.event_date as \"EventDate\", " +
      "p.http_method as \"HttpMethod\", " +
      "p.domain as \"Domain\", " +
      "p.entity as \"Entity\", " +
      "p.id as \"Id\", " +
      "p.status as \"Status\", " +
      "p.patch as \"Patch\" " +
      "FROM patchwork.patchwork_event_log as p " +
      "WHERE p.domain = @schema_name " +
      "AND p.entity = @table_name " +
      "AND p.id = @entity_id " +
      "AND p.event_date <= @as_of " +
      "ORDER BY p.event_date ",
      parameters);
  }
  internal override string BuildCountClause(Entity entity)
  {
    var schemaPrefix = string.IsNullOrEmpty(entity.SchemaName) ? string.Empty : $"{entity.SchemaName.ToLower()}.";
    return $"SELECT COUNT(*) FROM {schemaPrefix}{entity.Name.ToLower()} AS t_{entity.Name.ToLower()}";
  }
  internal override string BuildJoinClause(string includeString, Entity entity)
  {
    if (string.IsNullOrEmpty(includeString))
      throw new ArgumentException(includeString, nameof(includeString));

    try
    {
      var tokens = GetIncludeTokens(includeString, entity);
      var parser = new PostgreSqlIncludeTokenParser(tokens);
      return parser.Parse();
    } catch (Exception ex)
    {
      throw new ArgumentException($"Invalid include string: {ex.Message}", ex);
    }
  }
  internal override FilterStatement BuildWhereClause(string filterString, Entity entity)
  {
    try
    {
      var tokens = GetFilterTokens(filterString, entity);
      var parser = new PostgreSqlFilterTokenParser(tokens);
      var result = parser.Parse();

      return result;
    } catch (ArgumentException ex)
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
      var tokens = GetSortTokens(sort, entity);
      var parser = new PostgreSqlSortTokenParser(tokens);
      var orderby = parser.Parse();
      return $"ORDER BY {orderby}";
    } catch (ArgumentException ex)
    {
      throw new ArgumentException($"Invalid sort string: {ex.Message}", ex);
    }
  }
  internal override string BuildLimitOffsetClause(int limit, int offset)
  {
    try
    {
      var token = GetPagingToken(limit, offset);
      var parser = new PostgreSqlPagingParser(token);
      return parser.Parse();
    } catch (ArgumentException ex)
    {
      throw new ArgumentException($"Invalid limit or offset: {ex.Message}", ex);
    }
  }

  internal override string BuildInsertClause(Entity entity)
  {
    var schema = string.IsNullOrEmpty(entity.SchemaName) ? "" : $"{entity.SchemaName.ToLower()}.";
    return $"INSERT INTO {schema}{entity.Name.ToLower()} ";
  }
  internal override string BuildColumnListForInsert(Entity entity, Dictionary<string, object> parameters)
  {
    var list = entity.Columns
                                     .Where(x => !x.IsComputed)
                                     .Where(x => !x.IsAutoNumber)
                                     .Where(x => parameters.Keys.Contains(x.Name, StringComparer.OrdinalIgnoreCase))
                                     .OrderBy(x => x.IsPrimaryKey)
                                     .ThenBy(x => x.Name)
                                     .Select(x => x.Name.ToLower());

    return $"({string.Join(", ", list)})";
  }
  internal override string BuildParameterListForInsert(Entity entity, Dictionary<string, object> parameters)
  {
    var list = entity.Columns
                                     .Where(x => !x.IsComputed)
                                     .Where(x => !x.IsAutoNumber)
                                     .Where(x => parameters.Keys.Contains(x.Name, StringComparer.OrdinalIgnoreCase))
                                     .OrderBy(x => x.IsPrimaryKey)
                                     .ThenBy(x => x.Name)
                                     .Select(x => $"@{x.Name.ToLower()}");

    return $"VALUES ({string.Join(", ", list)}) RETURNING *";
  }

  internal override string BuildUpdateClause(Entity entity)
  {
    var schema = string.IsNullOrEmpty(entity.SchemaName) ? "" : $"{entity.SchemaName.ToLower()}.";
    return $"UPDATE {schema}{entity.Name} ";
  }
  internal override string BuildSetClause(Dictionary<string, object> parameters, Entity entity)
  {
    var sb = new StringBuilder();
    foreach (var parameter in parameters)
    {
      var col = entity.Columns.FirstOrDefault(c => c.Name.Equals(parameter.Key, StringComparison.OrdinalIgnoreCase));
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
    var schema = string.IsNullOrEmpty(entity.SchemaName) ? "" : $"{entity.SchemaName.ToLower()}.";
    return $"DELETE FROM {schema}{entity.Name.ToLower()} ";
  }
  internal override string BuildWherePkForDeleteClause(Entity entity) => BuildWherePkForUpdateClause(entity);
  protected override string GetInsertPatchTemplate() =>
    "INSERT INTO patchwork.patchwork_event_log (event_date, http_method, domain, entity, id, status, patch) " +
    "VALUES (CURRENT_TIMESTAMP AT TIME ZONE 'UTC', @httpmethod, @schemaname, @entityname, @id, @status, @patch) " +
    "RETURNING *";
}
