using System.Text.Json;
using Dapper;
using Json.More;
using Json.Patch;
using Patchwork.Authorization;
using Patchwork.DbSchema;
using Patchwork.SqlDialects;
using Patchwork.SqlStatements;
using static Dapper.SqlMapper;

namespace Patchwork.Repository;

public interface IPatchworkRepository
{

  GetListResult GetList(string schemaName, string entityName, string fields = "", string filter = "", string sort = "", int limit = 0, int offset = 0);
  GetResourceResult GetResource(string schemaName, string entityName, string id, string fields = "", string include = "", DateTimeOffset? asOf = null);
  PostResult PostResource(string schemaName, string entityName, JsonDocument jsonResourceRequestBody);
  PutResult PutResource(string schemaName, string entityName, string id, JsonDocument jsonResourceRequestBody);
  DeleteResult DeleteResource(string schemaName, string entityName, string id);
  PatchResourceResult PatchResource(string schemaName, int version, string entityName, string id, JsonPatch jsonPatchRequestBody);
  PatchListResult PatchList(string schemaName, int version, string entityName, JsonPatch jsonPatchRequestBody);
}


public class PatchworkRepository : IPatchworkRepository
{
  protected readonly IPatchworkAuthorization authorization;
  protected readonly ISqlDialectBuilder sqlDialect;
  private JsonDocument empty = JsonDocument.Parse("{}");

  public PatchworkRepository(IPatchworkAuthorization auth, ISqlDialectBuilder sql)
  {
    this.authorization = auth;
    this.sqlDialect = sql;
  }

  public GetListResult GetList(string schemaName, string entityName,
    string fields = "", string filter = "", string sort = "", int limit = 0, int offset = 0)
  {
    SelectStatement select = this.sqlDialect.BuildGetListSql(schemaName, entityName, fields, filter, sort, limit, offset);
    using ActiveConnection connect = this.sqlDialect.GetConnection();
    IEnumerable<dynamic> found = connect.Connection.Query(select.Sql, select.Parameters, connect.Transaction);
    string lastId = this.sqlDialect.GetPkValue(schemaName, entityName, found.Last());

    // TODO: Need to popuate the total records count.
    return new GetListResult(found.ToList(), 0, lastId, limit, offset);
  }

  public GetResourceResult GetResource(string schemaName, string entityName,
    string id, string fields = "", string include = "", DateTimeOffset? asOf = null)
  {
    // if (!authorization.GetPermissionToResource(schemaName, entityName, id, this.User).HasFlag(Permission.Get))
    //   return this.Unauthorized();

    SelectStatement select = this.sqlDialect.BuildGetSingleSql(schemaName, entityName, id, fields, include, asOf);
    using ActiveConnection connect = this.sqlDialect.GetConnection();
    dynamic? found = connect.Connection.QuerySingleOrDefault(select.Sql, select.Parameters, connect.Transaction);

    return new GetResourceResult(found);
  }

  public PostResult PostResource(string schemaName, string entityName, JsonDocument jsonResourceRequestBody)
  {
    Entity entity = this.sqlDialect.FindEntity(schemaName, entityName);
    InsertStatement sql = this.sqlDialect.BuildPostSingleSql(schemaName, entityName, jsonResourceRequestBody);
    using ActiveConnection connect = this.sqlDialect.GetConnection();
    try
    {
      IEnumerable<dynamic> found = connect.Connection.Query(sql.Sql, sql.Parameters, connect.Transaction);
      if (found.Count() < 1)
        throw new System.Data.InvalidExpressionException($"Insert Failed for {schemaName}:{entityName}");

      dynamic inserted = found.First();
      dynamic id = this.sqlDialect.GetPkValue(schemaName, entityName, inserted);

      JsonPatch patch;
      if (this.sqlDialect.HasPatchTrackingEnabled())
      {
        patch = this.sqlDialect.BuildDiffAsJsonPatch(empty, jsonResourceRequestBody);
        InsertStatement insertPatch = this.sqlDialect.GetInsertStatementForPatchworkLog(schemaName, entityName, id.ToString(), patch);
        IEnumerable<dynamic> patchCount = connect.Connection.Query(insertPatch.Sql, insertPatch.Parameters, connect.Transaction);
      }
      else patch = new JsonPatch();

      connect.Transaction.Commit();
      return new PostResult(id, inserted, patch);
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine(ex.Message);
      connect.Transaction.Rollback();
      throw;
    }
  }

  public PutResult PutResource(string schemaName, string entityName, string id, JsonDocument jsonResourceRequestBody)
  {
    SelectStatement select = this.sqlDialect.BuildGetSingleSql(schemaName, entityName, id);
    UpdateStatement update = this.sqlDialect.BuildPutSingleSql(schemaName, entityName, id, jsonResourceRequestBody);
    using ActiveConnection connect = this.sqlDialect.GetConnection();
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

      //TODO: Append this patch to the Patchwork Log

      connect.Transaction.Commit();

      return new PutResult(afterObject, patch);
    }
    catch (Exception)
    {
      connect.Transaction.Rollback();
      throw;
    }
  }

  public DeleteResult DeleteResource(string schemaName, string entityName, string id)
  {
    DeleteStatement delete = this.sqlDialect.BuildDeleteSingleSql(schemaName, entityName, id);
    using ActiveConnection connect = this.sqlDialect.GetConnection();
    try
    {
      int updated = connect.Connection.Execute(delete.Sql, delete.Parameters, connect.Transaction);
      if (updated < 1)
        throw new System.Data.RowNotInTableException();

      //TODO: Append this patch to the Patchwork Log

      connect.Transaction.Commit();

      return new DeleteResult(true, id);
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine(ex.Message);
      connect.Transaction.Rollback();
      return new DeleteResult(false, id);
    }
  }

  public PatchListResult PatchList(string schemaName, int version, string entityName, JsonPatch jsonPatchRequestBody)
  {
    throw new NotImplementedException();
  }

  public PatchResourceResult PatchResource(string schemaName, int version, string entityName, string id, JsonPatch jsonPatchRequestBody)
  {
    throw new NotImplementedException();
  }

  public bool AddPatchToLog(ActiveConnection connect, string schemaName, string entityName, string id, JsonPatch patch)
  {
    Entity entity = this.sqlDialect.FindEntity(schemaName, entityName);
    InsertStatement sql = this.sqlDialect.BuildPostSingleSql(schemaName, entityName, patch.ToJsonDocument());
    IEnumerable<dynamic> found = connect.Connection.Query(sql.Sql, sql.Parameters, connect.Transaction);
    return false;
  }
}

public record GetListResult(List<dynamic> Resources, long TotalCount, string LastId, int Limit, int Offset);
public record GetResourceResult(dynamic Resource);
public record PostResult(string id, dynamic Resource, JsonPatch Changes);
public record PutResult(dynamic Resource, JsonPatch Changes);
public record DeleteResult(bool Success, string Id);
public record PatchResourceResult(string id, dynamic Resource, JsonPatch Changes);
public record PatchListResult(List<PatchResourceResult> Inserted, List<PatchResourceResult> Updated, List<PatchResourceResult> Deleted);
