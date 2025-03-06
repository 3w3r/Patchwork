using System.Text.Json;
using Dapper;
using Json.Patch;
using Patchwork.SqlDialects;
using Patchwork.SqlStatements;
using static Dapper.SqlMapper;

namespace Patchwork.Repository;

public partial class PatchworkRepository : IPatchworkRepository
{
  public PutResult PutResource(string schemaName, string entityName, string id, JsonDocument jsonResourceRequestBody)
  {
    SelectResourceStatement select = this.sqlDialect.BuildGetSingleSql(schemaName, entityName, id);
    UpdateStatement update = this.sqlDialect.BuildPutSingleSql(schemaName, entityName, id, jsonResourceRequestBody);
    using WriterConnection connect = this.sqlDialect.GetWriterConnection();
    try
    {
      dynamic? beforeObject = connect.Connection.QuerySingleOrDefault(select.Sql, select.Parameters, connect.Transaction);
      if (beforeObject == null)
        throw new System.Data.RowNotInTableException();
      dynamic beforeUpdate = JsonSerializer.Serialize(beforeObject);
      int updated = connect.Connection.Execute(update.Sql, update.Parameters, connect.Transaction);
      dynamic? afterObject = connect.Connection.QuerySingleOrDefault(select.Sql, select.Parameters, connect.Transaction);
      if (afterObject == null)
        throw new System.Data.DataException("Update Failed");
      dynamic afterString = JsonSerializer.Serialize(afterObject);

      JsonPatch patch = this.sqlDialect.BuildDiffAsJsonPatch(beforeUpdate, afterString);

      if (this.sqlDialect.HasPatchTrackingEnabled())
        AddPatchToLog(connect, schemaName, entityName, id, patch);

      connect.Transaction.Commit();

      return new PutResult(afterObject, patch);
    }
    catch (Exception)
    {
      connect.Transaction.Rollback();
      throw;
    }
  }

}
