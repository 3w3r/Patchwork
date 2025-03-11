using MySqlConnector;

using Patchwork.DbSchema;
using Patchwork.SqlStatements;

using System.Data.Common;
using System.Text;

namespace Patchwork.SqlDialects.MySql;

public class MySqlDialectBuilder : SqlDialectBuilderBase
{
  public MySqlDialectBuilder(string connectionString, string defaultSchema = "public") : base(connectionString, defaultSchema) { }
  public MySqlDialectBuilder(DatabaseMetadata metadata, string defaultSchema = "public") : base(metadata, defaultSchema) { }

  /// <summary>
  /// Creates a new writer connection to the database using a MySqlConnection.
  /// </summary>
  /// <returns>A new WriterConnection object.</returns>
  public override WriterConnection GetWriterConnection()
  {
    // Create a new MySqlConnection object using the provided connection string.
    var c = new MySqlConnection(_connectionString);

    // Open the connection to the database.
    c.Open();

    // Begin a new transaction with the specified isolation level.
    DbTransaction t = c.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);

    // Return a new WriterConnection object with the opened connection and transaction.
    return new WriterConnection(c, t);
  }

  /// <summary>
  /// Creates a new reader connection to the database using a MySqlConnection.
  /// </summary>
  /// <returns>A new ReaderConnection object.</returns>
  public override ReaderConnection GetReaderConnection()
  {
    // Create a new MySqlConnection object using the provided connection string.
    var c = new MySqlConnection(_connectionString);

    // Open the connection to the database.
    c.Open();

    // Return a new ReaderConnection object with the opened connection.
    return new ReaderConnection(c);
  }

  internal override string BuildSelectClause(string fields, Entity entity)
  {
    // Check if the fields string is empty or contains the wildcard "*",
    // in which case select all columns from the table.
    var schemaPrefix = string.IsNullOrEmpty(entity.SchemaName) ? string.Empty : $"{entity.SchemaName}.";

    if (string.IsNullOrEmpty(fields) || fields.Contains("*"))
      return $"SELECT * FROM {schemaPrefix}{entity.Name} AS t_{entity.Name}";

    // Parse the fields string into a list of tokens,
    // each representing a column to be selected.
    var tokens = GetFieldTokens(fields, entity);

    // Parse the list of tokens into a formatted string of column names.
    var parser = new MySqlFieldsTokenParser(tokens);
    var fieldList = parser.Parse();

    // Return the SELECT statement with the formatted column names.
    return $"SELECT {fieldList} FROM {schemaPrefix}{entity.Name} AS t_{entity.Name}";
  }

  internal override SelectEventLogStatement GetSelectEventLog(Entity entity, string id, DateTimeOffset asOf)
  {
    // Create a dictionary of parameters for the SELECT statement.
    var parameters = new Dictionary<string, object>
    {
      { "schema_name", entity.SchemaName },
      { "table_name", entity.Name},
      { "entity_id", entity.PrimaryKey?.Name ?? "id"},
      { "as_of", asOf},
    };

    // Return a new SelectEventLogStatement object with the formatted SELECT statement
    // and the dictionary of parameters.
    return new SelectEventLogStatement(
      "SELECT `P`.`pk` as `Pk`, " +
      "`P`.`event_date` as `EventDate`, " +
      "`P`.`http_method` as `HttpMethod`, " +
      "`P`.`domain` as `Domain`, " +
      "`P`.`entity` as `Entity`, " +
      "`P`.`id` as `Id`, " +
      "`P`.`status` as `Status`, " +
      "`P`.`patch` as `Patch` " +
      "FROM `patchwork`.`patchwork_event_log` as `P` " +
      "WHERE `P`.`domain` = @schema_name " +
      "AND `P`.`entity` = @table_name " +
      "AND `P`.`id` = @entity_id " +
      "AND `P`.`event_date` <= @as_of " +
      "ORDER BY `p`.`event_date` ",
      parameters);
  }

  internal override string BuildCountClause(Entity entity)
  {
    // Check if the schema name is empty or null. If so, use an empty string as the schema prefix.
    // Otherwise, construct the schema prefix by concatenating the schema name with a dot.
    var schemaPrefix = string.IsNullOrEmpty(entity.SchemaName) ? string.Empty : $"{entity.SchemaName}.";

    // Return a SQL statement to count the number of rows in the specified entity's table.
    // The schema prefix is included if it exists, and the table name is appended with an alias "t_{entity.Name}".
    return $"SELECT COUNT(*) FROM {schemaPrefix}{entity.Name} AS t_{entity.Name}";
  }
  internal override string BuildJoinClause(string includeString, Entity entity)
  {
    // Check if the include string is empty or null. If so, return an empty string.
    if (string.IsNullOrEmpty(includeString))
      return string.Empty;

    try
    {
      // Parse the include string into a list of tokens.
      // Each token represents a relationship to be included in the SQL query.
      var tokens = GetIncludeTokens(includeString, entity);

      // Parse the list of tokens into a formatted SQL join clause.
      var parser = new MySqlIncludeTokenParser(tokens);
      return parser.Parse();
    } catch (Exception ex)
    {
      // If an exception occurs during parsing, throw a new ArgumentException with a descriptive error message.
      throw new ArgumentException($"Invalid include string: {ex.Message}", ex);
    }
  }
  internal override FilterStatement BuildWhereClause(string filterString, Entity entity)
  {
    // Try to parse the filter string into a list of tokens.
    // Each token represents a condition to be applied in the SQL query.
    try
    {
      var tokens = GetFilterTokens(filterString, entity);

      // Parse the list of tokens into a formatted SQL WHERE clause.
      var parser = new MySqlFilterTokenParser(tokens);
      return parser.Parse();
    }
    // If an exception occurs during parsing, throw a new ArgumentException with a descriptive error message.
    catch (ArgumentException ex)
    {
      throw new ArgumentException($"Invalid filter string: {ex.Message}", ex);
    }
  }
  internal override string BuildWherePkForGetClause(Entity entity)
  {
    // return a `where` clause to query by the primary key
    return $"WHERE t_{entity.Name}.{entity.PrimaryKey!.Name} = @id";
  }
  internal override string BuildOrderByClause(string sort, Entity entity)
  {
    // Try to parse the sort string into a list of tokens.
    // Each token represents a column and direction to be sorted in the SQL query.
    try
    {
      var tokens = GetSortTokens(sort, entity);

      // Parse the list of tokens into a formatted SQL ORDER BY clause.
      var parser = new MySqlSortTokenParser(tokens);
      var orderby = parser.Parse();

      // Return the ORDER BY clause with the formatted column names and directions.
      return $"ORDER BY {orderby}";
    }
    // If an exception occurs during parsing, throw a new ArgumentException with a descriptive error message.
    catch (ArgumentException ex)
    {
      throw new ArgumentException($"Invalid sort string: {ex.Message}", ex);
    }
  }
  internal override string BuildLimitOffsetClause(int limit, int offset)
  {
    // Try to create a paging token with the provided limit and offset.
    try
    {
      var token = GetPagingToken(limit, offset);

      // Parse the paging token into a formatted SQL LIMIT OFFSET clause.
      var parser = new MySqlPagingParser(token);
      return parser.Parse();
    }
    // If an exception occurs during token creation or parsing, throw a new ArgumentException with a descriptive error message.
    catch (ArgumentException ex)
    {
      throw new ArgumentException($"Invalid limit or offset: {ex.Message}", ex);
    }
  }

  internal override string BuildInsertClause(Entity entity)
  {
    // Determine the schema prefix for the SQL INSERT statement.
    // If the entity has a schema name, use it as the prefix.
    // If the entity does not have a schema name, use an empty string as the prefix.
    var schema = string.IsNullOrEmpty(entity.SchemaName) ? "" : $"{entity.SchemaName}.";

    // Return the SQL INSERT statement with the schema prefix and table name.
    return $"INSERT INTO {schema}{entity.Name} ";
  }
  internal override string BuildColumnListForInsert(Entity entity, Dictionary<string, object> parameters)
  {
    // Filter the columns of the entity to exclude computed and auto-number columns.
    // Only include columns that have corresponding parameters in the provided dictionary.
    // Order the selected columns by primary key first, then by name.
    var list = entity.Columns
                     .Where(x => !x.IsComputed)
                     .Where(x => !x.IsAutoNumber)
                     .Where(x => parameters.Keys.Contains(x.Name))
                     .OrderBy(x => x.IsPrimaryKey)
                     .ThenBy(x => x.Name)
                     .Select(x => $"{x.Name}");

    // Return the formatted list of column names enclosed in parentheses for the SQL INSERT statement.
    return $"({string.Join(", ", list)})";
  }
  internal override string BuildParameterListForInsert(Entity entity, Dictionary<string, object> parameters)
  {
    // Filter the columns of the entity to exclude computed and auto-number columns.
    // Only include columns that have corresponding parameters in the provided dictionary.
    // Order the selected columns by primary key first, then by name.
    var list = entity.Columns
                     .Where(x => !x.IsComputed)
                     .Where(x => !x.IsAutoNumber)
                     .Where(x => parameters.Keys.Contains(x.Name))
                     .OrderBy(x => x.IsPrimaryKey)
                     .ThenBy(x => x.Name)
                     .Select(x => $"@{x.Name}");

    // Return the formatted list of parameter names (prefixed with "@") enclosed in parentheses for the SQL INSERT statement.
    return $"VALUES ({string.Join(", ", list)})";
  }

  internal override string BuildUpdateClause(Entity entity)
  {
    // Determine the schema prefix for the SQL UPDATE statement.
    // If the entity has a schema name, use it as the prefix.
    // If the entity does not have a schema name, use an empty string as the prefix.
    var schema = string.IsNullOrEmpty(entity.SchemaName) ? "" : $"{entity.SchemaName}.";

    // Return the SQL UPDATE statement with the schema prefix and table name.
    return $"UPDATE {schema}{entity.Name} ";
  }
  internal override string BuildSetClause(Dictionary<string, object> parameters, Entity entity)
  {
    // Create a new StringBuilder to build the SQL SET clause.
    var sb = new StringBuilder();

    // Iterate through the provided parameters.
    foreach (var parameter in parameters)
    {
      // Find the corresponding column in the entity.
      var col = entity.Columns.FirstOrDefault(c => c.Name.Equals(parameter.Key, StringComparison.OrdinalIgnoreCase));

      // If the column does not exist, or if it is a primary key, auto-number, or computed column, skip it.
      if (col == null || col.IsPrimaryKey || col.IsAutoNumber || col.IsComputed)
        continue;

      // Append the column name and parameter name to the StringBuilder, separated by an equals sign and a comma.
      sb.Append($"\n  {col.Name} = @{parameter.Key},");
    }

    // Return the formatted SQL SET clause, with the column names and parameter names.
    // Trim any trailing comma and newline before returning the result.
    return $"SET {sb.ToString().TrimEnd(',')}\n";
  }
  // Build the WHERE clause for updating a row by the primary key.
  internal override string BuildWherePkForUpdateClause(Entity entity)
  {
    return $"WHERE {entity.PrimaryKey!.Name} = @id ";
  }

  // Build the SQL DELETE statement for the specified entity.
  internal override string BuildDeleteClause(Entity entity)
  {
    // Determine the schema prefix for the SQL DELETE statement.
    // If the entity has a schema name, use it as the prefix.
    // If the entity does not have a schema name, use an empty string as the prefix.
    var schema = string.IsNullOrEmpty(entity.SchemaName) ? "" : $"{entity.SchemaName}.";

    // Return the SQL DELETE statement with the schema prefix and table name.
    return $"DELETE FROM {schema}{entity.Name} ";
  }

  // Build the WHERE clause for deleting a row by the primary key.
  // This method simply calls the BuildWherePkForUpdateClause method.
  internal override string BuildWherePkForDeleteClause(Entity entity) => BuildWherePkForUpdateClause(entity);

  // Get the SQL template for inserting a patch event into the patchwork_event_log table.
  protected override string GetInsertPatchTemplate() =>
  "INSERT INTO patchwork.patchwork_event_log (event_date, http_method, domain, entity, id, status, patch) " +
  "VALUES (UTC_TIMESTAMP(), @httpmethod, @schemaname, @entityname, @id, @status, @patch)";
}
