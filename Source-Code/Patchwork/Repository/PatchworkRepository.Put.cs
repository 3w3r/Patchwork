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
  /// Updates a resource in the database based on the provided schema name, entity name, and ID.
  /// </summary>
  /// <param name="schemaName">The name of the database schema.</param>
  /// <param name="entityName">The name of the database entity.</param>
  /// <param name="id">The unique identifier of the resource to update.</param>
  /// <param name="jsonResourceRequestBody">The JSON representation of the resource to update.</param>
  /// <returns>A PutResult object containing the updated resource and the JSON Patch representing the changes.</returns>
  public PutResult PutResource(string schemaName, string entityName, string id, JsonDocument jsonResourceRequestBody)
  {
    // Building an SQL statement to select the resource before the update
    SelectResourceStatement select = this.sqlDialect.BuildGetSingleSql(schemaName, entityName, id);

    // Building an SQL statement to update the resource
    UpdateStatement update = this.sqlDialect.BuildPutSingleSql(schemaName, entityName, id, jsonResourceRequestBody);

    // Creating a WriterConnection object to establish a connection to the database for writing operations
    using WriterConnection connect = this.sqlDialect.GetWriterConnection();

    try
    {
      // Selecting the resource before the update
      dynamic? beforeObject = connect.Connection.QuerySingleOrDefault(select.Sql, select.Parameters, connect.Transaction);

      // Checking if the resource was found before the update
      if (beforeObject == null)
        throw new System.Data.RowNotInTableException();

      // Serializing the resource before the update
      dynamic beforeUpdate = JsonSerializer.Serialize(beforeObject);

      // Executing the update SQL statement
      int updated = connect.Connection.Execute(update.Sql, update.Parameters, connect.Transaction);

      // Selecting the resource after the update
      dynamic? afterObject = connect.Connection.QuerySingleOrDefault(select.Sql, select.Parameters, connect.Transaction);

      // Checking if the resource was found after the update
      if (afterObject == null)
        throw new System.Data.DataException("Update Failed");

      // Serializing the resource after the update
      dynamic afterString = JsonSerializer.Serialize(afterObject);

      // Building a JSON Patch representing the differences between the resource before and after the update
      JsonPatch patch = this.sqlDialect.BuildDiffAsJsonPatch(beforeUpdate, afterString);

      // Checking if patch tracking is enabled for the SQL dialect
      if (this.sqlDialect.HasPatchTrackingEnabled())
        // Adding the patch to the patch log
        AddPatchToLog(connect, schemaName, entityName, id, patch);

      // Committing the transaction
      connect.Transaction.Commit();

      // Returning the PutResult object containing the updated resource and the JSON Patch representing the changes
      return new PutResult(afterObject, patch);
    }
    catch (Exception)
    {
      // Rolling back the transaction in case of an exception
      connect.Transaction.Rollback();
      throw;
    }
  }
}
