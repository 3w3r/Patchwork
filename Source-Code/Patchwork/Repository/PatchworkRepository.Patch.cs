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
  public PatchResourceResult PatchResource(string schemaName, string entityName, string id, JsonPatch jsonPatchRequestBody)
  {
    using WriterConnection connect = this.sqlDialect.GetWriterConnection();
    try
    {
      PatchResourceResult result = PatchHandler(schemaName, entityName, id, jsonPatchRequestBody, connect);
      connect.Transaction.Commit();
      return result;
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine(ex.Message);
      connect.Transaction.Rollback();
      throw;
    }
  }

  private PatchResourceResult PatchHandler(string schemaName, string entityName, string id, JsonPatch jsonPatchRequestBody, WriterConnection connect)
  {
    SelectResourceStatement select = this.sqlDialect.BuildGetSingleSql(schemaName, entityName, id);
    dynamic? beforeObject = connect.Connection.QuerySingleOrDefault(select.Sql, select.Parameters, connect.Transaction);
    if (beforeObject == null)
      throw new System.Data.RowNotInTableException();
    dynamic beforeUpdate = JsonSerializer.Serialize(beforeObject);

    PatchResult afterUpdate = jsonPatchRequestBody.Apply(JsonNode.Parse(beforeUpdate));
    JsonDocument afterObject = afterUpdate.Result.ToJsonDocument();
    UpdateStatement update = this.sqlDialect.BuildPutSingleSql(schemaName, entityName, id, afterObject);

    int updated = connect.Connection.Execute(update.Sql, update.Parameters, connect.Transaction);

    JsonPatch patch = this.sqlDialect.BuildDiffAsJsonPatch(JsonDocument.Parse(beforeUpdate), afterObject);

    if (this.sqlDialect.HasPatchTrackingEnabled())
      AddPatchToLog(connect, schemaName, entityName, id, patch);

    PatchResourceResult result = new PatchResourceResult(id, afterObject, patch);
    return result;
  }

}
