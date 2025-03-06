using System.Text.Json;
using Json.Patch;
using Patchwork.Extensions;
using Patchwork.SqlDialects;

namespace Patchwork.Repository;

public partial class PatchworkRepository : IPatchworkRepository
{
  public PatchListResult PatchList(string schemaName, string entityName, JsonPatch jsonPatchRequestBody)
  {
    Dictionary<string, JsonPatch> patchDictionary = jsonPatchRequestBody.SplitById();

    using WriterConnection connect = this.sqlDialect.GetWriterConnection();

    try
    {
      PatchListResult results = new PatchListResult(new List<PatchResourceResult>(), new List<PatchResourceResult>(), new List<PatchDeleteResult>());

      foreach (KeyValuePair<string, JsonPatch> patch in patchDictionary)
      {
        if (patch.Key.StartsWith('-'))
        {
          //Create new record
          PostResult result = PostHandler(schemaName, entityName, JsonDocument.Parse(patch.Value.Operations.First().Value!.ToString()), connect);
          results.Inserted.Add(new PatchResourceResult(result.Id, result.Resource, result.Changes));
        }
        else if (patch.Value.Operations.First().Op.ToString() == "Remove")
        {
          // Delete record
          DeleteResult result = DeleteHandler(schemaName, entityName, patch.Key, connect);
          if (result.Success)
            results.Deleted.Add(new PatchDeleteResult(patch.Key, result.Changes ?? new JsonPatch()));
        }
        else
        {
          //Patch Record Normally
          PatchResourceResult result = PatchHandler(schemaName, entityName, patch.Key, patch.Value, connect);
          results.Updated.Add(result);
        }
      }
      connect.Transaction.Commit();
      return results;
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine(ex.Message);
      connect.Transaction.Rollback();
      throw;
    }
  }

}
