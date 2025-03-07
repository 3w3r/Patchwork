using Dapper;
using Json.Patch;
using Json.Pointer;
using Patchwork.SqlDialects;
using Patchwork.SqlStatements;
using static Dapper.SqlMapper;

namespace Patchwork.Repository;
public partial class PatchworkRepository : IPatchworkRepository
{
  /// <summary>
  /// Deletes a resource from the database based on the provided schema name, entity name, and ID.
  /// </summary>
  /// <param name="schemaName">The name of the database schema where the resource resides.</param>
  /// <param name="entityName">The name of the table or collection where the resource is stored.</param>
  /// <param name="id">The unique identifier of the resource to be deleted.</param>
  /// <returns>
  ///   An instance of <see cref="DeleteResult"/> containing the outcome of the deletion operation.
  ///   If the deletion is successful, the <see cref="DeleteResult.Success"/> property will be true,
  ///   and the <see cref="DeleteResult.Patch"/> property will contain a JSON Patch operation representing the deletion.
  ///   If the deletion fails, the <see cref="DeleteResult.Success"/> property will be false,
  ///   and the <see cref="DeleteResult.Patch"/> property will be null.
  /// </returns>
  public DeleteResult DeleteResource(string schemaName, string entityName, string id)
  {
    // Using a WriterConnection object to establish a connection to the database for executing the deletion operation.
    using WriterConnection connect = this.sqlDialect.GetWriterConnection();

    try
    {
      // Calling the DeleteHandler method to handle the deletion of the resource.
      DeleteResult result = DeleteHandler(schemaName, entityName, id, connect);

      // Committing the transaction if the deletion is successful.
      connect.Transaction.Commit();

      // Returning the DeleteResult object containing the outcome of the deletion operation.
      return result;
    }
    catch (Exception ex)
    {
      // Writing the exception message to the debug output if the deletion fails.
      System.Diagnostics.Debug.WriteLine(ex.Message);

      // Rolling back the transaction if the deletion fails.
      connect.Transaction.Rollback();

      // Returning a DeleteResult object indicating a failed deletion operation.
      return new DeleteResult(false, id, null);
    }
  }

  /// <summary>
  /// Handles the deletion of a resource from the database based on the provided schema name, entity name, and ID.
  /// </summary>
  /// <param name="schemaName">The name of the database schema where the resource resides.</param>
  /// <param name="entityName">The name of the table or collection where the resource is stored.</param>
  /// <param name="id">The unique identifier of the resource to be deleted.</param>
  /// <param name="connect">The database connection object used for executing the deletion.</param>
  /// <returns>
  ///   An instance of <see cref="DeleteResult"/> containing the outcome of the deletion operation.
  ///   If the deletion is successful, the <see cref="DeleteResult.Success"/> property will be true,
  ///   and the <see cref="DeleteResult.Patch"/> property will contain a JSON Patch operation representing the deletion.
  ///   If the deletion fails, the <see cref="DeleteResult.Success"/> property will be false,
  ///   and the <see cref="DeleteResult.Patch"/> property will be null.
  /// </returns>
  private DeleteResult DeleteHandler(string schemaName, string entityName, string id, WriterConnection connect)
  {
    // Building a DeleteStatement object using the provided schema name, entity name, and ID.
    DeleteStatement delete = this.sqlDialect.BuildDeleteSingleSql(schemaName, entityName, id);

    // Executing the SQL command represented by the DeleteStatement object,
    // using the provided connection, parameters, and transaction.
    // The number of rows affected by the execution is stored in the 'updated' variable.
    int updated = connect.Connection.Execute(delete.Sql, delete.Parameters, connect.Transaction);

    // Throwing a RowNotInTableException if the deletion operation did not affect any rows in the database.
    if (updated < 1)
      throw new System.Data.RowNotInTableException();

    // Initializing a JsonPatch object to null if patch tracking is not enabled.
    JsonPatch? patch = null;

    // If patch tracking is enabled, creating a new JsonPatch object representing a 'remove' operation
    // on the specified JSON pointer, and adding the patch to the database log.
    if (this.sqlDialect.HasPatchTrackingEnabled())
    {
      patch = new JsonPatch(PatchOperation.Remove(JsonPointer.Parse($"/{schemaName}/{entityName}/{id}")));
      AddPatchToLog(connect, schemaName, entityName, id, patch);
    }

    // Returning a DeleteResult object indicating a successful deletion operation,
    // along with the ID of the deleted resource and the JSON Patch operation representing the deletion.
    return new DeleteResult(true, id, patch);
  }
}