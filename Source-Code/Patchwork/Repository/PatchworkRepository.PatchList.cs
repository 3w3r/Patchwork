using System.Text.Json;
using Json.Patch;
using Patchwork.Extensions;
using Patchwork.SqlDialects;

namespace Patchwork.Repository;

public partial class PatchworkRepository : IPatchworkRepository
{
  /// <summary>
  /// Processes a batch of patch operations on a specified entity within a given schema.
  /// </summary>
  /// <param name="schemaName">The name of the database schema.</param>
  /// <param name="entityName">The name of the database entity.</param>
  /// <param name="jsonPatchRequestBody">The batch of patch operations to be applied.</param>
  /// <returns>A <see cref="PatchListResult"/> containing the results of the patch operations.</returns>
  public PatchListResult PatchList(string schemaName, string entityName, JsonPatch jsonPatchRequestBody)
  {
        // Splitting the JSON Patch request into individual patches based on the 'id' property
    Dictionary<string, JsonPatch> patchDictionary = jsonPatchRequestBody.SplitById();

    // Creating a WriterConnection object to establish a connection to the database for writing operations
    using WriterConnection connect = this.sqlDialect.GetWriterConnection();

    try
    {
      // Creating a PatchListResult object to store the results of the patch operations
      PatchListResult results = new PatchListResult(new List<PatchResourceResult>(), new List<PatchResourceResult>(), new List<PatchDeleteResult>());

      // Iterating through each patch in the patch dictionary
      foreach (KeyValuePair<string, JsonPatch> patch in patchDictionary)
      {
        if (patch.Key.StartsWith('-'))
        {
          // If the patch operation is to create a new record,
          // calling the PostHandler method to perform the operation
          PostResult result = PostHandler(schemaName, entityName, JsonDocument.Parse(patch.Value.Operations.First().Value!.ToString()), connect);

          // Adding the result of the post operation to the 'Inserted' list in the PatchListResult object
          results.Created.Add(new PatchResourceResult(result.Id, result.Resource, result.Changes));
        }
        else if (patch.Value.Operations.First().Op.ToString() == "Remove")
        {
          // If the patch operation is to delete a record,
          // calling the DeleteHandler method to perform the operation
          DeleteResult result = DeleteHandler(schemaName, entityName, patch.Key, connect);

          // If the delete operation was successful, adding the result to the 'Deleted' list in the PatchListResult object
          if (result.Success)
            results.Deleted.Add(new PatchDeleteResult(patch.Key, result.Changes ?? new JsonPatch()));
        }
        else
        {
          // If the patch operation is to update an existing record,
          // calling the PatchHandler method to perform the operation
          PatchResourceResult result = PatchHandler(schemaName, entityName, patch.Key, patch.Value, connect);

          // Adding the result of the patch operation to the 'Updated' list in the PatchListResult object
          results.Updated.Add(result);
        }
      }

      // Committing the transaction if all patch operations were successful
      connect.Transaction.Commit();

      // Returning the PatchListResult object containing the results of the patch operations
      return results;
    }
    catch (Exception ex)
    {
      // Writing the exception message to the debug output for troubleshooting purposes
      System.Diagnostics.Debug.WriteLine(ex.Message);

      // Rolling back the transaction if an exception occurred during the patch operations
      connect.Transaction.Rollback();

      // Re-throwing the exception to propagate it up the call stack
      throw;
    }
  }
}
