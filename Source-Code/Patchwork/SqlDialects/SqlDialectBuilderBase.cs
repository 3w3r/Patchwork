using Json.Patch;

using Patchwork.DbSchema;
using Patchwork.Expansion;
using Patchwork.Extensions;
using Patchwork.Fields;
using Patchwork.Filters;
using Patchwork.Paging;
using Patchwork.Sort;
using Patchwork.SqlStatements;

using System.Collections.Concurrent;
using System.Text.Json;

namespace Patchwork.SqlDialects;
public abstract class SqlDialectBuilderBase : ISqlDialectBuilder
{
  protected readonly string _connectionString;
  protected static ConcurrentDictionary<string, DatabaseMetadata> _metadataCache = new ConcurrentDictionary<string, DatabaseMetadata>();
  protected DatabaseMetadata? _metadata = null;
  public string DefaultSchemaName { get; init; }

  public SqlDialectBuilderBase(string connectionString, string defaultSchema)
  {
    _connectionString = connectionString;
    DefaultSchemaName = defaultSchema;
  }
  public SqlDialectBuilderBase(DatabaseMetadata metadata, string defaultSchema)
  {
    _connectionString = string.Empty;
    _metadata = metadata;
    DefaultSchemaName = defaultSchema;
  }

  public abstract WriterConnection GetWriterConnection();
  public abstract ReaderConnection GetReaderConnection();

  /// <summary>
  ///   Finds an entity (table or view) in the database schema based on the provided schema name and entity name.
  /// </summary>
  /// <param name="schemaName">The name of the schema where the entity resides.</param>
  /// <param name="entityName">The name of the entity (table or view) to find.</param>
  /// <returns>The found entity, or throws an exception if the entity was not found.</returns>
  public Entity FindEntity(string schemaName, string entityName)
  {
    if (string.IsNullOrEmpty(entityName))
      throw new ArgumentException(nameof(entityName));

    // Discover the database schema if it hasn't been discovered yet
    DiscoverSchema();

    // If the metadata is still null, then we cannot access the database schema
    if (_metadata == null)
      throw new ArgumentException("Cannot access database schema");

    // Try to find the entity in the tables of the specified schema
    var entity = _metadata.Schemas
                                .Where(s => s.Name.Equals(schemaName, StringComparison.OrdinalIgnoreCase))
                                .SelectMany(x => x.Tables)
                                .FirstOrDefault(t => t.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));

    // If the entity wasn't found in the tables, try to find it in the views of the specified schema
    if (entity == null)
      entity = _metadata.Schemas
                        .Where(s => s.Name.Equals(schemaName, StringComparison.OrdinalIgnoreCase))
                        .SelectMany(x => x.Views)
                        .FirstOrDefault(t => t.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));

    // If the entity still wasn't found, then throw an exception
    if (entity == null)
      throw new ArgumentException($"Invalid Table or View Name: {entityName}");

    // Return the found entity
    return entity;
  }

  /// <summary>
  ///   Gets the primary key value of an entity object based on the provided schema name and entity name.
  /// </summary>
  /// <param name="schemaName">The name of the schema where the entity resides.</param>
  /// <param name="entityName">The name of the entity (table or view) to find.</param>
  /// <param name="entityObject">The entity object from which to retrieve the primary key value.</param>
  /// <returns>The primary key value of the entity object, or an empty string if the primary key is not found.</returns>
  public virtual string GetPkValue(string schemaName, string entityName, object entityObject)
  {
    var entity = FindEntity(schemaName, entityName);

    // If the entity doesn't have a primary key, then return an empty string
    if (entity.PrimaryKey == null)
      return string.Empty;

    // If the entityObject was queried from Dapper, then it can be case as an IDictionary
    var row = entityObject as IDictionary<string, object?>;
    if (row != null && row.ContainsKey(entity.PrimaryKey.Name))
    {
      // If the primary key value is found in the dictionary, return it as a string
      return row[entity.PrimaryKey.Name]?.ToString() ?? string.Empty;
    } else
    {
      // If the entityObject wasn't queried from Dapper, then we have to use Reflection to get the value
      var type = entityObject.GetType();
      var property = type.GetProperty(entity.PrimaryKey.Name);

      // If the property is not found, then return an empty string
      if (property == null)
        return string.Empty;

      // If the property is found, then get its value and return it as a string
      var prop = property.GetValue(entityObject);
      return prop?.ToString() ?? string.Empty;
    }
  }

  /// <summary>
  /// Discovers the schema by connecting to the database and retrieving the schema information.
  /// </summary>
  /// <returns>The discovered database schema.</returns>
  public virtual DatabaseMetadata DiscoverSchema()
  {
    // If the schema has already been discovered and cached, return the cached schema
    if (_metadata != null || _metadataCache.TryGetValue(_connectionString, out _metadata))
    {
      return _metadata;
    }

    // Create an instance of the SchemaDiscoveryBuilder class
    var schemaDiscoveryBuilder = new SchemaDiscoveryBuilder();

    // Get a connection to the database using the GetWriterConnection method
    using var connection = GetWriterConnection();

    // Use the SchemaDiscoveryBuilder to read the schema from the database
    _metadata = schemaDiscoveryBuilder.ReadSchema(connection);

    // Cache the discovered schema for future use
    _metadataCache.TryAdd(_connectionString, _metadata);

    // Return the discovered schema
    return _metadata;
  }

  /// <summary>
  ///   Determines if patch tracking is enabled for the database schema.
  /// </summary>
  /// <returns>
  ///   Returns <see langword="true"/> if patch tracking is enabled; otherwise, <see langword="false"/>.
  /// </returns>
  /// <remarks>
  ///   This method first calls the <see cref="DiscoverSchema"/> method to ensure the database schema is discovered.
  ///   Then, it checks if the <see cref="_metadata"/> object is not null and if it has the
  ///   <see cref="DatabaseMetadata.HasPatchTracking"/> property set to <see langword="true"/>.
  /// </remarks>
  public virtual bool HasPatchTrackingEnabled()
  {
    if (_metadata == null)
      this.DiscoverSchema();
    return _metadata!.HasPatchTracking;
  }

  /// <summary>
  ///   Builds a SQL statement for retrieving a list of records from a specified entity.
  /// </summary>
  /// <param name="schemaName">The name of the schema where the entity resides.</param>
  /// <param name="entityName">The name of the entity (table or view) to retrieve records from.</param>
  /// <param name="fields">The fields to include in the SELECT statement. If not provided, all fields will be included.</param>
  /// <param name="filter">The filter to apply to the records. If not provided, all records will be retrieved.</param>
  /// <param name="sort">The sorting criteria for the records. If not provided, the records will be sorted in ascending order.</param>
  /// <param name="limit">The maximum number of records to retrieve. If not provided, all records will be retrieved.</param>
  /// <param name="offset">The number of records to skip before retrieving the records. If not provided, no records will be skipped.</param>
  /// <returns>A SQL statement for retrieving a list of records from the specified entity.</returns>
  public virtual SelectListStatement BuildGetListSql(string schemaName, string entityName, string fields = "", string filter = "", string sort = "", int limit = 0, int offset = 0)
  {
    if (string.IsNullOrEmpty(schemaName))
      throw new ArgumentException("Schema name is required.", nameof(schemaName));
    if (string.IsNullOrEmpty(entityName))
      throw new ArgumentException("Entity name is required.", nameof(entityName));

    // Find the entity in the database schema
    var entity = FindEntity(schemaName, entityName);

    // Build the SELECT clause for retrieving the specified fields
    var select = BuildSelectClause(fields, entity);

    // Build the COUNT clause for retrieving the total number of records
    var count = BuildCountClause(entity);

    // Build the WHERE clause for applying the filter
    var where = string.IsNullOrEmpty(filter) ? null : BuildWhereClause(filter, entity);

    // Build the ORDER BY clause for sorting the records
    var orderBy = BuildOrderByClause(sort, entity);

    // Build the LIMIT and OFFSET clauses for pagination
    var paging = BuildLimitOffsetClause(limit, offset);

    // Set the parameter data types for the WHERE clause
    where?.Parameters.SetParameterDataTypes(entity);

    // Return the SQL statement for retrieving the list of records
    return new SelectListStatement(
        $"{select} {where?.Sql} {orderBy} {paging}",
        $"{count} {where?.Sql}",
        where?.Parameters ?? new Dictionary<string, object>());
  }

  /// <summary>
  ///   Builds a SQL statement for retrieving a single record from a specified entity.
  /// </summary>
  /// <param name="schemaName">The name of the schema where the entity resides.</param>
  /// <param name="entityName">The name of the entity (table or view) to retrieve records from.</param>
  /// <param name="id">The unique identifier of the record to retrieve.</param>
  /// <param name="fields">The fields to include in the SELECT statement. If not provided, all fields will be included.</param>
  /// <param name="include">The related entities to include in the SELECT statement. If not provided, no related entities will be included.</param>
  /// <param name="asOf">The point in time to retrieve the record as of. If not provided, the current record will be retrieved.</param>
  /// <returns>A SQL statement for retrieving the single record.</returns>
  public virtual SelectResourceStatement BuildGetSingleSql(string schemaName, string entityName, string id, string fields = "", string include = "", DateTimeOffset? asOf = null)
  {
    // Validate input parameters
    if (string.IsNullOrEmpty(schemaName))
      throw new ArgumentException("Schema name is required.", nameof(schemaName));
    if (string.IsNullOrEmpty(entityName))
      throw new ArgumentException("Entity name is required.", nameof(entityName));

    // Find the entity in the database schema
    var entity = FindEntity(schemaName, entityName);

    // Build the SELECT clause for retrieving the specified fields
    var select = BuildSelectClause(fields, entity);

    // Build the JOIN clause for including related entities
    var join = string.IsNullOrEmpty(include) ? "" : BuildJoinClause(include, entity);

    // Build the WHERE clause for filtering the record by its unique identifier
    var where = BuildWherePkForGetClause(entity);

    // Create a dictionary of parameters for the SQL statement
    var parameters = new Dictionary<string, object> { { "id", id } };
    parameters.SetParameterDataTypes(entity);

    // Return the SQL statement for retrieving the single record
    return new SelectResourceStatement($"{select} {join} {where}", parameters);
  }

  /// <summary>
  ///   Builds a SQL statement for retrieving a single record from a specified entity based on the provided ID and asOf timestamp.
  /// </summary>
  /// <param name="schemaName">The name of the schema where the entity resides.</param>
  /// <param name="entityName">The name of the entity (table or view) to retrieve records from.</param>
  /// <param name="id">The unique identifier of the record to retrieve.</param>
  /// <param name="asOf">The point in time to retrieve the record as of.</param>
  /// <returns>A SQL statement for retrieving the single record.</returns>
  public virtual SelectEventLogStatement BuildGetEventLogSql(string schemaName, string entityName, string id, DateTimeOffset asOf)
  {
    // Validate input parameters
    if (string.IsNullOrEmpty(schemaName))
      throw new ArgumentException("Schema name is required.", nameof(schemaName));
    if (string.IsNullOrEmpty(entityName))
      throw new ArgumentException("Entity name is required.", nameof(entityName));

    // Find the entity in the database schema
    var entity = FindEntity(schemaName, entityName);

    // Return the SQL statement for retrieving the event log based on the provided ID and asOf timestamp
    return GetSelectEventLog(entity, id, asOf);
  }

  /// <summary>
  ///   Builds a SQL statement for retrieving a single record from a specified entity based on the provided ID and asOf timestamp.
  /// </summary>
  /// <param name="entity">The entity (table or view) to retrieve records from.</param>
  /// <param name="id">The unique identifier of the record to retrieve.</param>
  /// <param name="asOf">The point in time to retrieve the record as of.</param>
  /// <returns>A SQL statement for retrieving the single record based on the provided ID and asOf timestamp.</returns>
  internal abstract SelectEventLogStatement GetSelectEventLog(Entity entity, string id, DateTimeOffset asOf);

  /// <summary>
  ///   Builds a SQL statement for creating a new record in a specified entity.
  /// </summary>
  /// <param name="schemaName">The name of the schema where the entity resides.</param>
  /// <param name="entityName">The name of the entity (table or view) to create a new record in.</param>
  /// <param name="jsonResourceRequestBody">The JSON representation of the resource to be created.</param>
  /// <returns>A SQL statement for creating a new record.</returns>
  /// <exception cref="ArgumentException">Thrown when the schema name or entity name is not provided.</exception>
  /// <exception cref="ArgumentException">Thrown when the JSON resource body is not provided.</exception>
  /// <exception cref="UnauthorizedAccessException">Thrown when the entity is read-only.</exception>
  public virtual InsertStatement BuildPostSingleSql(string schemaName, string entityName, JsonDocument jsonResourceRequestBody)
  {
    // Validate input parameters
    if (string.IsNullOrEmpty(schemaName))
      throw new ArgumentException("Schema name is required.", nameof(schemaName));
    if (string.IsNullOrEmpty(entityName))
      throw new ArgumentException("Entity name is required.", nameof(entityName));
    if (jsonResourceRequestBody == null)
      throw new ArgumentException("JsonResourceRequestBody name is required.", nameof(jsonResourceRequestBody));

    // Find the entity in the database schema
    var entity = FindEntity(schemaName, entityName);

    // Check if the entity is read-only
    if (entity.IsReadOnly)
      throw new UnauthorizedAccessException($"Cannot create records in {entity.Name}");

    // Build the INSERT clause for the SQL statement
    var insert = BuildInsertClause(entity);

    // Create a dictionary of parameters for the SQL statement
    var parameters = new Dictionary<string, object>();

    // Add the JSON resource body to the parameters dictionary
    parameters.AddJsonResourceToDictionary(jsonResourceRequestBody);

    // Set the parameter data types for the SQL statement
    parameters.SetParameterDataTypes(entity);

    // Build the column list for the SQL statement
    var columnList = BuildColumnListForInsert(entity, parameters);

    // Build the parameter list for the SQL statement
    var paramsList = BuildParameterListForInsert(entity, parameters);

    // Return the SQL statement for creating a new record
    return new InsertStatement($"{insert} {columnList} {paramsList}", HttpMethodsEnum.Post, parameters);
  }

  /// <summary>
  ///   Builds a SQL statement for updating an existing record in a specified entity.
  /// </summary>
  /// <param name="schemaName">The name of the schema where the entity resides.</param>
  /// <param name="entityName">The name of the entity (table or view) to update a record in.</param>
  /// <param name="id">The unique identifier of the record to update.</param>
  /// <param name="jsonResourceRequestBody">The JSON representation of the resource to be updated.</param>
  /// <returns>A SQL statement for updating an existing record.</returns>
  /// <exception cref="ArgumentException">Thrown when the schema name, entity name, or ID is not provided.</exception>
  /// <exception cref="ArgumentException">Thrown when the JSON resource body is not provided.</exception>
  /// <exception cref="UnauthorizedAccessException">Thrown when the entity is read-only.</exception>
  public virtual UpdateStatement BuildPutSingleSql(string schemaName, string entityName, string id, JsonDocument jsonResourceRequestBody)
  {
    // Validate input parameters
    if (string.IsNullOrEmpty(schemaName))
      throw new ArgumentException("Schema name is required.", nameof(schemaName));
    if (string.IsNullOrEmpty(entityName))
      throw new ArgumentException("Entity name is required.", nameof(entityName));
    if (string.IsNullOrEmpty(id))
      throw new ArgumentException("Entity Id is required.", nameof(id));
    if (jsonResourceRequestBody == null)
      throw new ArgumentException("JsonResourceRequestBody is required.", nameof(jsonResourceRequestBody));

    // Find the entity in the database schema
    var entity = FindEntity(schemaName, entityName);

    // Check if the entity is read-only
    if (entity.IsReadOnly)
      throw new UnauthorizedAccessException($"Cannot update records in {entity.Name}");

    // Build the UPDATE clause for the SQL statement
    var update = BuildUpdateClause(entity);

    // Build the WHERE clause for filtering the record by its unique identifier
    var where = BuildWherePkForUpdateClause(entity);

    // Create a dictionary of parameters for the SQL statement
    var parameters = new Dictionary<string, object>() {
      { "id", id }
    };

    // Add the JSON resource body to the parameters dictionary
    parameters.AddJsonResourceToDictionary(jsonResourceRequestBody);

    // Set the parameter data types for the SQL statement
    parameters.SetParameterDataTypes(entity);

    // Build the SET clause for the SQL statement
    var sets = BuildSetClause(parameters, entity);

    // Return the SQL statement for updating an existing record
    return new UpdateStatement($"{update} {sets} {where}", parameters);
  }

  /// <summary>
  ///   Builds a SQL statement for deleting a single record from a specified entity based on the provided ID.
  /// </summary>
  /// <param name="schemaName">The name of the schema where the entity resides.</param>
  /// <param name="entityName">The name of the entity (table or view) to delete records from.</param>
  /// <param name="id">The unique identifier of the record to delete.</param>
  /// <returns>A SQL statement for deleting the single record based on the provided ID.</returns>
  public virtual DeleteStatement BuildDeleteSingleSql(string schemaName, string entityName, string id)
  {
    // Validate input parameters
    if (string.IsNullOrEmpty(schemaName))
      throw new ArgumentException("Schema name is required.", nameof(schemaName));

    if (string.IsNullOrEmpty(entityName))
      throw new ArgumentException("Entity name is required.", nameof(entityName));

    if (string.IsNullOrEmpty(id))
      throw new ArgumentException("Entity Id name is required.", nameof(id));

    // Find the entity in the database schema
    var entity = FindEntity(schemaName, entityName);

    // Check if the entity is read-only
    if (entity.IsReadOnly)
      throw new UnauthorizedAccessException($"Cannot delete records from {entity.Name}");

    // Build the DELETE clause for the SQL statement
    var delete = BuildDeleteClause(entity);

    // Build the WHERE clause for filtering the record by its unique identifier
    var where = BuildWherePkForDeleteClause(entity);

    // Create a dictionary of parameters for the SQL statement
    var parameters = new Dictionary<string, object>() { { "id", id } };

    // Set the parameter data types for the SQL statement
    parameters.SetParameterDataTypes(entity);

    // Return the SQL statement for deleting the single record
    return new DeleteStatement($"{delete} {where}", parameters);
  }

  /// <summary>
  ///   Builds a JSON Patch object representing the differences between two JSON strings.
  /// </summary>
  /// <param name="original">The original JSON string.</param>
  /// <param name="modified">The modified JSON string.</param>
  /// <returns>A JSON Patch object representing the differences between the original and modified JSON strings.</returns>
  public virtual JsonPatch BuildDiffAsJsonPatch(string original, string modified)
  {
    // Parse the original and modified JSON strings into JSON documents
    return BuildDiffAsJsonPatch(JsonDocument.Parse(original), JsonDocument.Parse(modified));
  }

  /// <summary>
  ///   Builds a JSON Patch object representing the differences between two JSON documents.
  /// </summary>
  /// <param name="original">The original JSON document.</param>
  /// <param name="modified">The modified JSON document.</param>
  /// <returns>A JSON Patch object representing the differences between the original and modified JSON documents.</returns>
  public virtual JsonPatch BuildDiffAsJsonPatch(JsonDocument original, JsonDocument modified)
  {
    // Create a JSON Patch object representing the differences between the original and modified JSON documents
    var patch = original.CreatePatch(modified);

    // Return the JSON Patch object
    return patch;
  }

  /// <summary>
  ///   Gets an InsertStatement object for inserting a record into the Patchwork Log table based on the provided schema name, entity name, ID, and JSON Patch object.
  /// </summary>
  /// <param name="schemaName">The name of the schema where the record will be inserted.</param>
  /// <param name="entityName">The name of the entity (table or view) where the record will be inserted.</param>
  /// <param name="id">The unique identifier of the record to insert.</param>
  /// <param name="jsonPatchRequestBody">The JSON Patch object representing the changes to be applied to the record.</param>
  /// <returns>An InsertStatement object for inserting the record into the Patchwork Log table.</returns>
  public virtual InsertStatement GetInsertStatementForPatchworkLog(HttpMethodsEnum httpMethod, string schemaName, string entityName, string id, JsonPatch jsonPatchRequestBody)
  {
    // Create a dictionary of parameters for the InsertStatement
    var insertParams = new Dictionary<string, object>();
    insertParams.Add("httpmethod", (int)httpMethod);
    insertParams.Add("schemaname", schemaName);
    insertParams.Add("entityname", entityName);
    insertParams.Add("id", id);
    insertParams.Add("status", 0);
    insertParams.Add("patch", JsonSerializer.Serialize(jsonPatchRequestBody));

    // Return the InsertStatement object using the template and parameters
    return new InsertStatement(this.GetInsertPatchTemplate(), httpMethod, insertParams);
  }

  /// <summary> Abstract method to get the SQL template for inserting a record with a JSON Patch object </summary>
  protected abstract string GetInsertPatchTemplate();

  /// <summary> Internal abstract method to build the SELECT clause for the SQL statement </summary>
  internal abstract string BuildSelectClause(string fields, Entity entity);

  /// <summary> Internal abstract method to build the COUNT clause for the SQL statement </summary>
  internal abstract string BuildCountClause(Entity entity);

  /// <summary> Internal abstract method to build the JOIN clause for the SQL statement </summary>
  internal abstract string BuildJoinClause(string includeString, Entity entity);

  /// <summary> Internal abstract method to build the WHERE clause for the SQL statement </summary>
  internal abstract FilterStatement BuildWhereClause(string filterString, Entity entity);

  /// <summary> Internal abstract method to build the WHERE clause for filtering records by their unique identifier in the SQL statement </summary>
  internal abstract string BuildWherePkForGetClause(Entity entity);

  /// <summary> Internal abstract method to build the ORDER BY clause for the SQL statement </summary>
  internal abstract string BuildOrderByClause(string sort, Entity entity);

  /// <summary> Internal abstract method to build the LIMIT OFFSET clause for the SQL statement </summary>
  internal abstract string BuildLimitOffsetClause(int limit, int offset);

  /// <summary> Internal abstract method to build the INSERT clause for the SQL statement </summary>
  internal abstract string BuildInsertClause(Entity entity);

  /// <summary> Internal abstract method to build the column list for the INSERT clause in the SQL statement </summary>
  internal abstract string BuildColumnListForInsert(Entity entity, Dictionary<string, object> parameters);

  /// <summary> Internal abstract method to build the parameter list for the INSERT clause in the SQL statement </summary>
  internal abstract string BuildParameterListForInsert(Entity entity, Dictionary<string, object> parameters);

  /// <summary> Internal abstract method to build the UPDATE clause for the SQL statement </summary>
  internal abstract string BuildUpdateClause(Entity entity);

  /// <summary> Internal abstract method to build the SET clause for the UPDATE clause in the SQL statement </summary>
  internal abstract string BuildSetClause(Dictionary<string, object> parameters, Entity entity);

  /// <summary> Internal abstract method to build the WHERE clause for filtering records by their unique identifier in the UPDATE SQL statement </summary>
  internal abstract string BuildWherePkForUpdateClause(Entity entity);

  /// <summary> Internal abstract method to build the DELETE clause for the SQL statement </summary>
  internal abstract string BuildDeleteClause(Entity entity);

  /// <summary> Internal abstract method to build the WHERE clause for filtering records by their unique identifier in the DELETE SQL statement </summary>
  internal abstract string BuildWherePkForDeleteClause(Entity entity);

  /// <summary>
  ///   Gets a list of FieldsToken objects representing the fields to be included in the SQL statement.
  /// </summary>
  /// <param name="fields">The string representing the fields to be included in the SQL statement.</param>
  /// <param name="entity">The entity (table or view) for which the fields are being retrieved.</param>
  /// <returns>A list of FieldsToken objects representing the fields to be included in the SQL statement.</returns>
  protected List<FieldsToken> GetFieldTokens(string fields, Entity entity)
  {
    // Create a FieldsLexer object to tokenize the fields string
    var lexer = new FieldsLexer(fields, entity, DiscoverSchema());

    // Tokenize the fields string using the FieldsLexer object
    var tokens = lexer.Tokenize();

    // Return the list of FieldsToken objects representing the fields to be included in the SQL statement
    return tokens;
  }

  /// <summary>
  ///   Gets a list of FilterToken objects representing the filters to be applied in the SQL statement.
  /// </summary>
  /// <param name="filterString">The string representing the filters to be applied in the SQL statement.</param>
  /// <param name="entity">The entity (table or view) for which the filters are being retrieved.</param>
  /// <returns>A list of FilterToken objects representing the filters to be applied in the SQL statement.</returns>
  protected List<FilterToken> GetFilterTokens(string filterString, Entity entity)
  {
    // Validate input parameters
    if (string.IsNullOrWhiteSpace(filterString))
      throw new ArgumentException("No input string");

    if (entity == null)
      throw new ArgumentNullException(nameof(entity));

    // Create a FilterLexer object to tokenize the filter string
    var lexer = new FilterLexer(filterString, entity, DiscoverSchema());

    // Tokenize the filter string using the FilterLexer object
    var tokens = lexer.Tokenize();

    // Validate the tokens
    if (tokens.Count == 0)
      throw new ArgumentException("No valid tokens found");

    // Iterate through the tokens and perform additional validation
    foreach (var token in tokens)
    {
      // Check if the token is an identifier and if the column exists on the entity
      if (
           token.Type == FilterTokenType.Identifier &&
           !entity.Columns.Any(c => c.Name.Equals(token.Value, StringComparison.OrdinalIgnoreCase))
         )
      {
        // Throw an exception if the column does not exist on the entity
        throw new ArgumentException($@"{token.Value} is not a column on {entity.Name}", nameof(filterString));
      }
    }

    // Return the list of FilterToken objects representing the filters to be applied in the SQL statement
    return tokens;
  }

  /// <summary>
  ///   Gets a list of IncludeToken objects representing the includes to be applied in the SQL statement.
  /// </summary>
  /// <param name="includeString">The string representing the includes to be applied in the SQL statement.</param>
  /// <param name="entity">The entity (table or view) for which the includes are being retrieved.</param>
  /// <returns>A list of IncludeToken objects representing the includes to be applied in the SQL statement.</returns>
  protected List<IncludeToken> GetIncludeTokens(string includeString, Entity entity)
  {
    // Validate input parameters
    if (string.IsNullOrEmpty(includeString))
      throw new ArgumentException(nameof(includeString));

    if (entity == null)
      throw new ArgumentNullException(nameof(entity));

    // Create an IncludeLexer object to tokenize the include string
    var lexer = new IncludeLexer(includeString, entity, DiscoverSchema());

    // Tokenize the include string using the IncludeLexer object
    var tokens = lexer.Tokenize();

    // Return the list of IncludeToken objects representing the includes to be applied in the SQL statement
    return tokens;
  }

  /// <summary>
  ///   Gets a list of SortToken objects representing the sorting to be applied in the SQL statement.
  /// </summary>
  /// <param name="sort">The string representing the sorting to be applied in the SQL statement.</param>
  /// <param name="entity">The entity (table or view) for which the sorting is being retrieved.</param>
  /// <returns>A list of SortToken objects representing the sorting to be applied in the SQL statement.</returns>
  protected List<SortToken> GetSortTokens(string sort, Entity entity)
  {
    // Validate input parameters
    if (entity == null)
      throw new ArgumentNullException(nameof(entity));

    // Create a SortLexer object to tokenize the sort string
    var lexer = new SortLexer(sort, entity, DiscoverSchema());

    // Tokenize the sort string using the SortLexer object
    var tokens = lexer.Tokenize();

    // Return the list of SortToken objects representing the sorting to be applied in the SQL statement
    return tokens;
  }

  /// <summary>
  ///   Gets a PagingToken object representing the paging settings for the SQL statement.
  /// </summary>
  /// <param name="limit">The maximum number of records to be returned in a single page.</param>
  /// <param name="offset">The number of records to skip before starting to return records in the current page.</param>
  /// <returns>A PagingToken object representing the paging settings for the SQL statement.</returns>
  protected PagingToken GetPagingToken(int limit, int offset)
  {
    // Create a new PagingToken object using the provided limit and offset values
    return new PagingToken(limit, offset);
  }
}
