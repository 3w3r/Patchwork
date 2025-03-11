using System.Text.Json;
using Dapper;
using Json.Patch;
using Patchwork.DbSchema;
using Patchwork.SqlDialects;
using Patchwork.SqlStatements;
using static Dapper.SqlMapper;

namespace Patchwork.Repository;

public partial class PatchworkRepository : IPatchworkRepository
{

  /// <summary>
  /// Posts a new resource to the specified schema and entity.
  /// </summary>
  /// <param name="schemaName">The name of the database schema where the resource will be inserted.</param>
  /// <param name="entityName">The name of the table or collection where the resource will be inserted.</param>
  /// <param name="jsonResourceRequestBody">The JSON representation of the resource to be inserted.</param>
  /// <returns>A <see cref="PostResult"/> object containing the ID of the newly inserted resource, the resource itself, and a JSON Patch representing the changes made.</returns>

  public PostResult PostResource(string schemaName, string entityName, JsonDocument jsonResourceRequestBody)
  {
    // Finding the entity (table or collection) in the specified schema
    Entity entity = this.sqlDialect.FindEntity(schemaName, entityName);

    // Creating a WriterConnection object to establish a connection to the database for writing operations
    using WriterConnection connect = this.sqlDialect.GetWriterConnection();

    try
    {
      // Calling the PostHandler method to perform the post operation
      PostResult result = PostHandler(schemaName, entityName, jsonResourceRequestBody, connect);

      // Committing the transaction if the post operation was successful
      connect.Transaction.Commit();

      // Returning the result of the post operation
      return result;
    }
    catch (Exception ex)
    {
      // Writing the exception message to the debug output for troubleshooting purposes
      System.Diagnostics.Debug.WriteLine(ex.Message);

      // Rolling back the transaction if an exception occurred during the post operation
      connect.Transaction.Rollback();

      // Re-throwing the exception to propagate it up the call stack
      throw;
    }
  }

  private PostResult PostHandler(string schemaName, string entityName, JsonDocument jsonResourceRequestBody, WriterConnection connect)
  {
    // Building an InsertStatement object to represent the SQL statement for inserting a new resource
    InsertStatement sql = this.sqlDialect.BuildPostSingleSql(schemaName, entityName, jsonResourceRequestBody);

    // Executing the SQL statement using Dapper and storing the result in the 'found' variable
    IEnumerable<dynamic> found = connect.Connection.Query(sql.Sql, sql.Parameters, connect.Transaction);

    // Checking if the insert operation was successful by verifying that at least one row was affected
    if (found.Count() < 1)
      throw new System.Data.InvalidExpressionException($"Insert Failed for {schemaName}:{entityName}");

    // Extracting the first (and only) row from the result and assigning it to the 'inserted' variable
    dynamic inserted = found.First();

    // Extracting the primary key value from the inserted row using the appropriate method of the SQL dialect
    dynamic id = this.sqlDialect.GetPkValue(schemaName, entityName, inserted);

    // Creating a JsonPatch object to represent the changes made to the resource
    JsonPatch patch;

    // Checking if patch tracking is enabled for the SQL dialect
    if (this.sqlDialect.HasPatchTrackingEnabled())
    {
      // If patch tracking is enabled, building a JSON Patch representing the differences between the original and new resource
      patch = this.sqlDialect.BuildDiffAsJsonPatch(empty, jsonResourceRequestBody);

      // Adding the patch to the patch log using the appropriate method of the SQL dialect
      AddPatchToLog(connect, HttpMethodsEnum.Post, schemaName, entityName, id, patch);
    }
    else
    {
      // If patch tracking is not enabled, creating an empty JSON Patch
      patch = new JsonPatch();
    }

    // Returning a PostResult object containing the ID of the newly inserted resource, the resource itself, and the JSON Patch representing the changes made
    return new PostResult(id, inserted, patch);
  }
}
