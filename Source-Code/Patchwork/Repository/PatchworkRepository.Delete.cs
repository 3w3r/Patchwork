using Dapper;
using Json.Patch;
using Json.Pointer;
using Patchwork.SqlDialects;
using Patchwork.SqlStatements;
using static Dapper.SqlMapper;

namespace Patchwork.Repository;
public partial class PatchworkRepository : IPatchworkRepository
{
  public DeleteResult DeleteResource(string schemaName, string entityName, string id)
  {
    using WriterConnection connect = this.sqlDialect.GetWriterConnection();
    try
    {
      DeleteResult result = DeleteHandler(schemaName, entityName, id, connect);
      connect.Transaction.Commit();
      return result;
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine(ex.Message);
      connect.Transaction.Rollback();
      return new DeleteResult(false, id, null);
    }
  }

  private DeleteResult DeleteHandler(string schemaName, string entityName, string id, WriterConnection connect)
  {
    DeleteStatement delete = this.sqlDialect.BuildDeleteSingleSql(schemaName, entityName, id);
    int updated = connect.Connection.Execute(delete.Sql, delete.Parameters, connect.Transaction);
    if (updated < 1)
      throw new System.Data.RowNotInTableException();

    JsonPatch? patch = null;
    if (this.sqlDialect.HasPatchTrackingEnabled())
    {
      patch = new JsonPatch(PatchOperation.Remove(JsonPointer.Parse($"/{schemaName}/{entityName}/{id}")));
      AddPatchToLog(connect, schemaName, entityName, id, patch);
    }

    return new DeleteResult(true, id, patch);
  }
}