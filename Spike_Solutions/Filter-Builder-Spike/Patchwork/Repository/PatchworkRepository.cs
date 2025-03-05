using System.ComponentModel.Design;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dapper;
using Json.More;
using Json.Patch;
using Json.Pointer;
using Patchwork.Authorization;
using Patchwork.DbSchema;
using Patchwork.Paging;
using Patchwork.Extensions;
using Patchwork.SqlDialects;
using Patchwork.SqlStatements;
using static Dapper.SqlMapper;

namespace Patchwork.Repository;

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
    SelectListStatement select = this.sqlDialect.BuildGetListSql(schemaName, entityName, fields, filter, sort, limit, offset);
    using ReaderConnection connect = this.sqlDialect.GetReaderConnection();
    IEnumerable<dynamic> found = connect.Connection.Query(select.Sql, select.Parameters);
    long count = connect.Connection.ExecuteScalar<long>(select.CountSql, select.Parameters);

    string lastId = found.Any()
      ? this.sqlDialect.GetPkValue(schemaName, entityName, found.Last())
      : string.Empty;

    return new GetListResult(found.ToList(), count, lastId, PagingToken.ParseLimit(limit), offset);
  }

  public GetResourceResult GetResource(string schemaName, string entityName,
    string id, string fields = "", string include = "", DateTimeOffset? asOf = null)
  {
    //TODO: Need to implement recovery of object from Patchwork Event Log
    if (asOf != null || asOf < DateTime.UtcNow.AddSeconds(-1))
      throw new NotImplementedException();

    //TODO: Need to implement repacking of the result into an object hierarchy breakdown instead of flat record results.
    if(!string.IsNullOrEmpty(include)) 
      throw new NotImplementedException();
    
    SelectResourceStatement select = this.sqlDialect.BuildGetSingleSql(schemaName, entityName, id, fields, include, asOf);
    using ReaderConnection connect = this.sqlDialect.GetReaderConnection();
    IEnumerable<dynamic> found = connect.Connection.Query(select.Sql, select.Parameters);

    if(found.Count()==1)
      return new GetResourceResult(found.FirstOrDefault());
    else
      //TODO: Instead of returning the list, we need to convert the flat records into an object hierarchy
      return new GetResourceResult(found.ToList());
  }

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

  public DeleteResult DeleteResource(string schemaName, string entityName, string id)
  {
    DeleteStatement delete = this.sqlDialect.BuildDeleteSingleSql(schemaName, entityName, id);
    using WriterConnection connect = this.sqlDialect.GetWriterConnection();
    try
    {
      int updated = connect.Connection.Execute(delete.Sql, delete.Parameters, connect.Transaction);
      if (updated < 1)
        throw new System.Data.RowNotInTableException();

      if (this.sqlDialect.HasPatchTrackingEnabled())
      {
        JsonPatch patch = new JsonPatch(PatchOperation.Remove(JsonPointer.Parse($"/{schemaName}/{entityName}/{id}")));
        AddPatchToLog(connect, schemaName, entityName, id, patch);
      }

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

  public PatchListResult PatchList(string schemaName, string entityName, JsonPatch jsonPatchRequestBody)
  {
        Dictionary<string, JsonPatch> patchDictionary = jsonPatchRequestBody.SplitById();

        using WriterConnection connect = this.sqlDialect.GetWriterConnection();

        try
        {
            PatchListResult results = new PatchListResult(new List<PatchResourceResult>(), new List<PatchResourceResult>(), new List<PatchDeleteResult>());

            foreach (var patch in patchDictionary)
            {
                if (patch.Key.StartsWith('-'))
                {
                    //Create new record


                    PostHandler(schemaName, entityName, JsonDocument.Parse(patch.Value.Operations.First().Value!.ToString()), connect);
                    //results.Inserted.Add(new PatchResourceResult())
                }
                else if (patch.Value.Operations.First().Op.ToString() == "Remove")
                {

                    DeleteStatement delete = this.sqlDialect.BuildDeleteSingleSql(schemaName, entityName, patch.Key);
                    int updated = connect.Connection.Execute(delete.Sql, delete.Parameters, connect.Transaction);
                    if (updated < 1)
                        throw new System.Data.RowNotInTableException();
                    results.Deleted.Add(new PatchDeleteResult(patch.Key, patch.Value));
                }
                else
                {
                    //Patch Normally
                    //results.Updated.Add(new PatchResourceResult())
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

  public PatchResourceResult PatchResource(string schemaName, string entityName, string id, JsonPatch jsonPatchRequestBody)
  {
    SelectResourceStatement select = this.sqlDialect.BuildGetSingleSql(schemaName, entityName, id);

    using WriterConnection connect = this.sqlDialect.GetWriterConnection();
    try
    {
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

      connect.Transaction.Commit();

      return new PatchResourceResult(id, afterObject, patch);
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine(ex.Message);
      connect.Transaction.Rollback();
      throw;
    }
  }

  public bool AddPatchToLog(WriterConnection connect, string schemaName, string entityName, string id, JsonPatch patch)
  {
    InsertStatement insertPatch = this.sqlDialect.GetInsertStatementForPatchworkLog(schemaName, entityName, id.ToString(), patch);
    int patchedCount = connect.Connection.Execute(insertPatch.Sql, insertPatch.Parameters, connect.Transaction);
    return patchedCount > 0;
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
          //InsertStatement insertPatch = this.sqlDialect.GetInsertStatementForPatchworkLog(schemaName, entityName, id.ToString(), patch);
          //IEnumerable<dynamic> patchCount = connect.Connection.Query(insertPatch.Sql, insertPatch.Parameters, connect.Transaction);
          AddPatchToLog(connect, schemaName, entityName, id, patch);
      }
      else
          patch = new JsonPatch();
      return new PostResult(id, inserted, patch);
  }
}