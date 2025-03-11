using Microsoft.Data.SqlClient;

using Patchwork.DbSchema;
using Patchwork.SqlStatements;

using System.Data.Common;
using System.Text;

namespace Patchwork.SqlDialects.MsSql;

public class MsSqlDialectBuilder : SqlDialectBuilderBase
{
  public MsSqlDialectBuilder(string connectionString, string defaultSchema = "dbo") : base(connectionString, defaultSchema) { }

  public MsSqlDialectBuilder(DatabaseMetadata metadata, string defaultSchema = "dbo") : base(metadata, defaultSchema) { }

  /// <summary>
  /// Creates a new WriterConnection for executing SQL commands that modify the database.
  /// </summary>
  /// <returns>A new WriterConnection.</returns>
  public override WriterConnection GetWriterConnection()
  {
    // Create a new SqlConnection with the provided connection string
    var c = new SqlConnection(_connectionString);

    // Open the connection
    c.Open();

    // Begin a new transaction with ReadUncommitted isolation level
    DbTransaction t = c.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);

    // Return a new WriterConnection with the opened connection and transaction
    return new WriterConnection(c, t);
  }
  /// <summary>
  /// Creates a new ReaderConnection for executing SQL commands that read from the database.
  /// </summary>
  /// <returns>A new ReaderConnection.</returns>
  public override ReaderConnection GetReaderConnection()
  {
    // Create a new SqlConnection with the provided connection string
    var c = new SqlConnection(_connectionString);

    // Open the connection
    c.Open();

    // Return a new ReaderConnection with the opened connection
    return new ReaderConnection(c);
  }

  internal override string BuildSelectClause(string fields, Entity entity)
  {
    // Check if the fields string is empty or contains the wildcard '*'
    // If so, select all columns from the table
    var schemaPrefix = string.IsNullOrEmpty(entity.SchemaName) ? string.Empty : $"[{entity.SchemaName}].";

    if (string.IsNullOrEmpty(fields) || fields.Contains("*"))
      return $"SELECT * FROM {schemaPrefix}[{entity.Name}] AS [t_{entity.Name}]";

    // If the fields string is not empty and does not contain the wildcard '*',
    // parse the fields string into tokens and generate the field list for the SELECT statement
    var tokens = GetFieldTokens(fields, entity);
    var parser = new MsSqlFieldsTokenParser(tokens);
    var fieldList = parser.Parse();

    // Return the SELECT statement with the generated field list and table name
    return $"SELECT {fieldList} FROM {schemaPrefix}[{entity.Name}] AS [t_{entity.Name}]";
  }
  internal override SelectEventLogStatement GetSelectEventLog(Entity entity, string id, DateTimeOffset asOf)
  {
    // Create a new dictionary to store the parameter values for the SELECT statement
    var parameters = new Dictionary<string, object>
    {
      { "schema_name", entity.SchemaName },
      { "table_name", entity.Name},
      { "entity_id", entity.PrimaryKey?.Name ?? "id"},
      { "as_of", asOf},
    };

    // Return a new SelectEventLogStatement object with the SQL query and parameter values
    // The SQL query selects specific columns from the patchwork_event_log table,
    // filters the results based on the provided schema name, table name, entity ID, and event date,
    // and orders the results by the event date
    return new SelectEventLogStatement(
      "SELECT [P].[pk] as [Pk], " +
      "[P].[event_date] as [EventDate], " +
      "[P].[http_method] as [HttpMethod], " +
      "[P].[domain] as [Domain], " +
      "[P].[entity] as [Entity], " +
      "[P].[id] as [Id], " +
      "[P].[status] as [Status], " +
      "[P].[patch] as [Patch] " +
      "FROM [patchwork].[patchwork_event_log] as [P] " +
      "WHERE [P].[domain] = @schema_name " +
      "AND [P].[entity] = @table_name " +
      "AND [P].[id] = @entity_id " +
      "AND [P].[event_date] <= @as_of " +
      "ORDER BY [P].[event_date] ",
      parameters);
  }

  internal override string BuildCountClause(Entity entity)
  {
    // Create a new string variable to store the schema prefix for the SQL query
    // If the entity's schema name is empty, the prefix will be an empty string
    // Otherwise, the prefix will be in the format "[schema_name]."
    var schemaPrefix = string.IsNullOrEmpty(entity.SchemaName) ? string.Empty : $"[{entity.SchemaName}].";

    // Return a SELECT COUNT(*) SQL query that counts the number of rows in the specified table
    // The query includes the schema prefix if it is not empty
    return $"SELECT COUNT(*) FROM {schemaPrefix}[{entity.Name}] AS [t_{entity.Name}]";
  }
  internal override string BuildJoinClause(string includeString, Entity entity)
  {
    // Check if the include string is empty
    // If it is, throw an ArgumentException with the name of the includeString parameter
    if (string.IsNullOrEmpty(includeString))
      throw new ArgumentException(nameof(includeString));

    try
    {
      // Parse the include string into tokens using the GetIncludeTokens method
      var tokens = GetIncludeTokens(includeString, entity);
      // Create a new instance of the MsSqlIncludeTokenParser class with the tokens
      var parser = new MsSqlIncludeTokenParser(tokens);
      // Call the Parse method of the parser to generate the SQL JOIN clause
      return parser.Parse();
    } catch (Exception ex)
    {
      // If an exception occurs during the parsing process,
      // throw a new ArgumentException with a custom error message
      // Include the original exception message in the error message
      throw new ArgumentException($"Invalid include string: {ex.Message}", ex);
    }
  }
  internal override FilterStatement BuildWhereClause(string filterString, Entity entity)
  {
    // Try to parse the filter string into tokens using the GetFilterTokens method
    try
    {
      var tokens = GetFilterTokens(filterString, entity);
      // Create a new instance of the MsSqlFilterTokenParser class with the tokens
      var parser = new MsSqlFilterTokenParser(tokens);
      // Call the Parse method of the parser to generate the FilterStatement
      var result = parser.Parse();

      // Return the generated FilterStatement
      return result;
    }
    // If an ArgumentException occurs during the parsing process,
    // throw a new ArgumentException with a custom error message
    // Include the original exception message in the error message
    catch (ArgumentException ex)
    {
      throw new ArgumentException($"Invalid filter string: {ex.Message}", ex);
    }
  }
  internal override string BuildWherePkForGetClause(Entity entity)
  {
    // Return a string that represents the WHERE clause for updating a specific record in the database
    // The WHERE clause filters the records based on the primary key column of the entity
    // The placeholder @id is used to parameterize the value of the primary key
    // The table name is prefixed with "t_" to indicate that it is a table alias
    // The primary key column name is enclosed in square brackets to ensure proper SQL syntax
    return $"WHERE [t_{entity.Name}].[{entity.PrimaryKey!.Name}] = @id";
  }
  internal override string BuildOrderByClause(string sort, Entity entity)
  {
    try
    {
      // Try to parse the sort string into tokens using the GetSortTokens method
      var tokens = GetSortTokens(sort, entity);
      // Create a new instance of the MsSortTokenParser class with the tokens
      var parser = new MsSortTokenParser(tokens);
      // Call the Parse method of the parser to generate the ORDER BY clause
      var orderby = parser.Parse();

      // Return the generated ORDER BY clause
      // The ORDER BY clause specifies the order in which the records should be returned
      // The generated clause is enclosed in double quotes to ensure proper SQL syntax
      return $"ORDER BY {orderby}";
    }
    // If an ArgumentException occurs during the parsing process,
    // throw a new ArgumentException with a custom error message
    // Include the original exception message in the error message
    catch (ArgumentException ex)
    {
      throw new ArgumentException($"Invalid sort string: {ex.Message}", ex);
    }
  }
  internal override string BuildLimitOffsetClause(int limit, int offset)
  {
    // Attempt to get the paging token using the provided limit and offset
    var token = GetPagingToken(limit, offset);

    // Instantiate a new MsSqlPagingParser object with the obtained token
    var parser = new MsSqlPagingParser(token);

    // Parse the token and return the parsed result
    // If an ArgumentException is thrown during parsing, rethrow it with a more descriptive error message
    try
    {
      return parser.Parse();
    } catch (ArgumentException ex)
    {
      throw new ArgumentException($"Invalid limit or offset: {ex.Message}", ex);
    }
  }

  internal override string BuildInsertClause(Entity entity)
  {
    // Check if the schema name is provided and if it's empty. If it's empty, assign an empty string to the 'schema' variable.
    // If the schema name is not empty, wrap it with square brackets and assign it to the 'schema' variable.
    var schema = string.IsNullOrEmpty(entity.SchemaName) ? "" : $"[{entity.SchemaName}].";

    // Construct the SQL INSERT INTO statement using the 'schema' and the entity's name.
    // If a schema name is provided, it will be included in the statement.
    return $"INSERT INTO {schema}[{entity.Name}] ";
  }
  internal override string BuildColumnListForInsert(Entity entity, Dictionary<string, object> parameters)
  {
    // Filter the columns of the entity to exclude computed columns, auto-number columns, and columns not present in the provided parameters
    var list = entity.Columns
                     .Where(x => !x.IsComputed)
                     .Where(x => !x.IsAutoNumber)
                     .Where(x => parameters.Keys.Contains(x.Name))
                     .OrderBy(x => x.IsPrimaryKey)
                     .ThenBy(x => x.Name)
                     // Select the column names, wrapping them with square brackets
                     .Select(x => $"[{x.Name}]");

    // Construct the SQL OUTPUT clause for the INSERT INTO statement, specifying that all inserted columns should be returned
    // Join the selected column names with commas and include them in the OUTPUT clause
    return $"({string.Join(", ", list)}) OUTPUT inserted.*";
  }
  internal override string BuildParameterListForInsert(Entity entity, Dictionary<string, object> parameters)
  {
    // Filter the columns of the entity to exclude computed columns, auto-number columns, and columns not present in the provided parameters
    var list = entity.Columns
                     .Where(x => !x.IsComputed)
                     .Where(x => !x.IsAutoNumber)
                     .Where(x => parameters.Keys.Contains(x.Name))
                     .OrderBy(x => x.IsPrimaryKey)
                     .ThenBy(x => x.Name)
                     // Select the column names, prefixing them with '@' to represent parameters
                     .Select(x => $"@{x.Name}");

    // Construct the SQL VALUES clause for the INSERT INTO statement, specifying the parameter values for each column
    // Join the selected parameter names with commas and include them in the VALUES clause
    return $"VALUES ({string.Join(", ", list)})";
  }

  internal override string BuildUpdateClause(Entity entity)
  {
    // Check if the schema name is provided, if not, use an empty string.
    // If a schema name is provided, wrap it with square brackets to adhere to SQL Server syntax.
    var schema = string.IsNullOrEmpty(entity.SchemaName) ? "" : $"[{entity.SchemaName}].";

    // Construct the SQL UPDATE statement using the provided schema and table name.
    // If no schema is provided, an empty string is used.
    return $"UPDATE {schema}[{entity.Name}] ";
  }
  internal override string BuildSetClause(Dictionary<string, object> parameters, Entity entity)
  {
    // Initialize a new StringBuilder to construct the SQL SET clause.
    var sb = new StringBuilder();

    // Iterate through each parameter provided.
    foreach (var parameter in parameters)
    {
      // Find the corresponding column in the entity's columns collection.
      var col = entity.Columns.FirstOrDefault(c => c.Name.Equals(parameter.Key, StringComparison.OrdinalIgnoreCase));

      // If the column is not found, or if it is a primary key, auto-number, or computed column, skip it.
      if (col == null || col.IsPrimaryKey || col.IsAutoNumber || col.IsComputed)
        continue;

      // Append the column name and parameter placeholder to the StringBuilder.
      sb.Append($"\n  [{col.Name}] = @{parameter.Key},");
    }

    // Trim any trailing comma and newline characters from the StringBuilder, and return the resulting SQL SET clause.
    return $"SET {sb.ToString().TrimEnd(',')}\n";
  }
  internal override string BuildWherePkForUpdateClause(Entity entity)
  {
    // Return the SQL WHERE clause with the primary key column name and parameter placeholder.
    // The primary key column name is obtained from the entity's PrimaryKey property.
    // The parameter placeholder is "@id" to match the parameter used in the calling method.
    return $"WHERE [{entity.PrimaryKey!.Name}] = @id ";
  }

  internal override string BuildDeleteClause(Entity entity)
  {
    // Check if the schema name is provided, if not, use an empty string.
    // If a schema name is provided, wrap it with square brackets to adhere to SQL Server syntax.
    var schema = string.IsNullOrEmpty(entity.SchemaName) ? "" : $"[{entity.SchemaName}].";

    // Construct the SQL DELETE statement using the provided schema and table name.
    // If no schema is provided, an empty string is used.
    return $"DELETE FROM {schema}[{entity.Name}] ";
  }

  // Override the BuildWherePkForDeleteClause method to use the same logic as BuildWherePkForUpdateClause.
  // This ensures that the correct primary key condition is used when deleting records.
  internal override string BuildWherePkForDeleteClause(Entity entity) => BuildWherePkForUpdateClause(entity);

  // Define a custom SQL template for the INSERT PATCH operation.
  // This template inserts a new record into the patchwork_event_log table with the current UTC date and time,
  // the schema name, entity name, ID, and patch data.
  // The OUTPUT clause is used to return the newly inserted record.
  protected override string GetInsertPatchTemplate() =>
    "INSERT INTO [patchwork].[patchwork_event_log] ([event_date], [http_method], [domain], [entity], [id], [status], [patch]) " +
    "OUTPUT INSERTED.* " +
    "VALUES (SYSUTCDATETIME(), @httpmethod, @schemaname, @entityname, @id, @status, @patch) ";
}
