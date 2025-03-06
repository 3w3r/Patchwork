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
  public PostResult PostResource(string schemaName, string entityName, JsonDocument jsonResourceRequestBody)
  {
    Entity entity = this.sqlDialect.FindEntity(schemaName, entityName);
    using WriterConnection connect = this.sqlDialect.GetWriterConnection();
    try
    {
      PostResult result = PostHandler(schemaName, entityName, jsonResourceRequestBody, connect);

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

  private PostResult PostHandler(string schemaName, string entityName, JsonDocument jsonResourceRequestBody, WriterConnection connect)
  {
    InsertStatement sql = this.sqlDialect.BuildPostSingleSql(schemaName, entityName, jsonResourceRequestBody);
    IEnumerable<dynamic> found = connect.Connection.Query(sql.Sql, sql.Parameters, connect.Transaction);
    if (found.Count() < 1)
      throw new System.Data.InvalidExpressionException($"Insert Failed for {schemaName}:{entityName}");

    dynamic inserted = found.First();
    dynamic id = this.sqlDialect.GetPkValue(schemaName, entityName, inserted);

    JsonPatch patch;
    if (this.sqlDialect.HasPatchTrackingEnabled())
    {
      patch = this.sqlDialect.BuildDiffAsJsonPatch(empty, jsonResourceRequestBody);
      AddPatchToLog(connect, schemaName, entityName, id, patch);
    }
    else
      patch = new JsonPatch();
    return new PostResult(id, inserted, patch);
  }
}
