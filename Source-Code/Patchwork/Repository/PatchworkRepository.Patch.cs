using System.Text.Json;
using System.Text.Json.Nodes;
using Dapper;
using Json.More;
using Json.Patch;
using Patchwork.SqlDialects;
using Patchwork.SqlStatements;
using static Dapper.SqlMapper;

namespace Patchwork.Repository;

public partial class PatchworkRepository : IPatchworkRepository
{
  /// <summary>
  /// Updates an existing resource in the database based on the provided JSON Patch request.
  /// </summary>
  /// <param name="schemaName">The name of the database schema where the resource resides.</param>
  /// <param name="entityName">The name of the table or collection where the resource resides.</param>
  /// <param name="id">The unique identifier of the resource to be updated.</param>
  /// <param name="jsonPatchRequestBody">The JSON Patch request containing the changes to be applied.</param>
  /// <returns>A <see cref="PatchResourceResult"/> object containing the updated resource and the applied JSON Patch.</returns>
  public PatchResourceResult PatchResource(string schemaName, string entityName, string id, JsonPatch jsonPatchRequestBody)
  {
    // Creating a WriterConnection object to establish a connection to the database for writing operations
    using WriterConnection connect = this.sqlDialect.GetWriterConnection();

    try
    {
      // Calling the PatchHandler method to perform the actual patch operation
      PatchResourceResult result = PatchHandler(schemaName, entityName, id, jsonPatchRequestBody, connect);

      // Committing the transaction if the patch operation was successful
      connect.Transaction.Commit();

      // Returning the result of the patch operation
      return result;
    }
    catch (Exception ex)
    {
      // Writing the exception message to the debug output for troubleshooting purposes
      System.Diagnostics.Debug.WriteLine(ex.Message);

      // Rolling back the transaction if an exception occurred during the patch operation
      connect.Transaction.Rollback();

      // Re-throwing the exception to propagate it up the call stack
      throw;
    }
  }

  private PatchResourceResult PatchHandler(string schemaName, string entityName, string id, JsonPatch jsonPatchRequestBody, WriterConnection connect)
  {
    // Building a SelectResourceStatement object to retrieve the existing resource from the database
    SelectResourceStatement select = this.sqlDialect.BuildGetSingleSql(schemaName, entityName, id);

    // Executing the SQL command represented by the SelectResourceStatement object,
    // using the provided connection and transaction.
    // The result is stored in the 'beforeObject' variable.
    dynamic? beforeObject = connect.Connection.QuerySingleOrDefault(select.Sql, select.Parameters, connect.Transaction);

    // Throwing a RowNotInTableException if the resource was not found in the database
    if (beforeObject == null)
      throw new System.Data.RowNotInTableException();

    // Serializing the 'beforeObject' dynamic object to a JSON string
    dynamic beforeUpdate = JsonSerializer.Serialize(beforeObject);

    // Applying the provided JSON Patch request to the 'beforeUpdate' JSON string
    PatchResult afterUpdate = jsonPatchRequestBody.Apply(JsonNode.Parse(beforeUpdate));

    // Converting the result of the patch application to a JsonDocument
    JsonDocument afterObject = afterUpdate.Result.ToJsonDocument();

    // Building an UpdateStatement object to update the existing resource in the database
    UpdateStatement update = this.sqlDialect.BuildPutSingleSql(schemaName, entityName, id, afterObject);

    // Executing the SQL command represented by the UpdateStatement object,
    // using the provided connection and transaction.
    int updated = connect.Connection.Execute(update.Sql, update.Parameters, connect.Transaction);

    // Building a JSON Patch representing the difference between the 'beforeUpdate' and 'afterObject' JSON strings
    JsonPatch patch = this.sqlDialect.BuildDiffAsJsonPatch(JsonDocument.Parse(beforeUpdate), afterObject);

    // Adding the JSON Patch to the patch tracking log if patch tracking is enabled
    if (this.sqlDialect.HasPatchTrackingEnabled())
      AddPatchToLog(connect, schemaName, entityName, id, patch);

    // Creating a PatchResourceResult object and returning it
    PatchResourceResult result = new PatchResourceResult(id, afterObject, patch);
    return result;
  }
}
