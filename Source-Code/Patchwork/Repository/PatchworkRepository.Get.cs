using System.Text.Json;
using Dapper;
using Json.Patch;
using Patchwork.SqlDialects;
using Patchwork.SqlStatements;
using static Dapper.SqlMapper;

namespace Patchwork.Repository;
public partial class PatchworkRepository : IPatchworkRepository
{
  /// <summary>
  ///   Retrieves a single resource from the database based on the provided schema name, entity name, and ID.
  /// </summary>
  /// <param name="schemaName">The name of the database schema.</param>
  /// <param name="entityName">The name of the database table or entity.</param>
  /// <param name="id">The unique identifier of the resource.</param>
  /// <param name="fields">A comma-separated list of fields to retrieve. If empty, all fields will be returned.</param>
  /// <param name="include">A comma-separated list of related resources to include in the result. Not implemented yet.</param>
  /// <returns>A <see cref="GetResourceResult"/> containing the retrieved resource or a list of resources if multiple resources are found.</returns>
  /// <exception cref="NotImplementedException">Thrown if the 'include' parameter is not empty.</exception>
  public GetResourceResult GetResource(string schemaName, string entityName,
    string id, string fields = "", string include = "")
  {
    // TODO: Need to implement repacking of the result into an object hierarchy breakdown instead of flat record results.
    if (!string.IsNullOrEmpty(include))
      throw new NotImplementedException();

    // Build the SQL statement for retrieving a single resource
    SelectResourceStatement select = this.sqlDialect.BuildGetSingleSql(schemaName, entityName, id, fields, include);

    // Get a reader connection to execute the SQL statement
    using ReaderConnection connect = this.sqlDialect.GetReaderConnection();

    // Execute the SQL statement and retrieve the resource
    IEnumerable<dynamic> found = connect.Connection.Query(select.Sql, select.Parameters);

    // If a single resource is found, return it as a GetResourceResult
    if (found.Count() == 1)
    {
      return new GetResourceResult(found.FirstOrDefault());
    }
    else
    {
      // TODO: Instead of returning the list, we need to convert the flat records into an object hierarchy
      return new GetResourceResult(found.ToList());
    }
  }

  /// <summary>
  ///   Retrieves a specific resource from the database based on the provided schema name, entity name, ID, and asOf date.
  ///   The function reconstructs the resource's state at the specified point in time by applying the JSON Patches recorded in the event log.
  /// </summary>
  /// <param name="schemaName">The name of the database schema.</param>
  /// <param name="entityName">The name of the database table or entity.</param>
  /// <param name="id">The unique identifier of the resource.</param>
  /// <param name="asOf">The point in time to retrieve the resource's state.</param>
  /// <returns>A <see cref="GetResourceAsOfResult"/> containing the reconstructed resource, the count of log events, and the event date of the last log event.</returns>
  public GetResourceAsOfResult GetResourceAsOf(string schemaName, string entityName, string id, DateTimeOffset asOf)
  {
    // Build the SQL statement for retrieving the event log for a specific resource
    SelectEventLogStatement select = this.sqlDialect.BuildGetEventLogSql(schemaName, entityName, id, asOf);

    // Get a reader connection to execute the SQL statement
    using ReaderConnection connect = this.sqlDialect.GetReaderConnection();

    // Execute the SQL statement and retrieve the event log
    IEnumerable<PatchworkLogEvent> found = connect.Connection.Query<PatchworkLogEvent>(select.Sql, select.Parameters);

    // Initialize the resource as an empty JSON document
    JsonDocument resource = JsonDocument.Parse("{}");

    // Iterate through each log event in the event log
    foreach (PatchworkLogEvent log in found)
    {
      // Deserialize the JSON Patch from the log event
      JsonPatch? patch = JsonSerializer.Deserialize<JsonPatch>(log.Patch);
      if (patch == null)
        throw new InvalidDataException($"The JSON Patch could not be read  \n {log}");

      // If the JSON Patch contains a single operation
      if (patch.Operations.Count == 1)
      {
        PatchOperation op = patch.Operations.First();

        // If the operation is a remove operation and the path contains the schema name, entity name, and ID
        if (op.Op == OperationType.Remove
          && op.Path.Contains(schemaName, StringComparer.OrdinalIgnoreCase)
          && op.Path.Contains(entityName, StringComparer.OrdinalIgnoreCase)
          && op.Path.Contains(id, StringComparer.OrdinalIgnoreCase))
        {
          // Reset the resource to an empty JSON document
          resource = JsonDocument.Parse("{}");
          continue;
        }
      }

      // Apply the JSON Patch to the current resource
      JsonDocument? changed = patch.Apply(resource);
      if (changed == null)
        throw new InvalidDataException($"The JSON Patch could not be applied \n {log}");

      // Update the resource with the changed JSON document
      resource = changed;
    }

    // Return the GetResourceAsOfResult with the final resource, the count of log events, and the event date of the last log event
    return new GetResourceAsOfResult(resource, found.Count(), found.Last().EventDate);
  }
}
