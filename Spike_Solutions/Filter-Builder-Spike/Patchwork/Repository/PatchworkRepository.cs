using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Json.Patch;
using Microsoft.Identity.Client;
using Patchwork.Authorization;
using Patchwork.DbSchema;
using Patchwork.SqlDialects;
using Patchwork.SqlStatements;
using static System.Net.Mime.MediaTypeNames;
using static Dapper.SqlMapper;
using static Patchwork.Repository.PatchworkRepository;

namespace Patchwork.Repository;

public interface IPatchworkRepository
{

  List<dynamic> GetList(string schemaName, string entityName,
    string fields = "", string filter = "", string sort = "", int limit = 0, int offset = 0);

  dynamic? GetResource(string schemaName, string entityName, string id,
    string fields = "", string include = "", DateTimeOffset? asOf = null);

  dynamic PostResource(string schemaName, string entityName, JsonDocument jsonResourceRequestBody);

  UpdatedResource PutResource(string schemaName, string entityName, string id, JsonDocument jsonResourceRequestBody);

  bool DeleteResource(string schemaName, string entityName, string id);
}

public record PatchworkModificationResult();

public class PatchworkRepository : IPatchworkRepository
{
  protected readonly IPatchworkAuthorization authorization;
  protected readonly ISqlDialectBuilder sqlDialect;

  public PatchworkRepository(IPatchworkAuthorization auth, ISqlDialectBuilder sql)
  {
    this.authorization = auth;
    this.sqlDialect = sql;
  }

  public List<dynamic> GetList(string schemaName, string entityName,
    string fields = "", string filter = "", string sort = "", int limit = 0, int offset = 0)
  {
    // if (!authorization.GetPermissionToCollection(schemaName, entityName, this.User).HasFlag(Permission.Get))
    //   return this.Unauthorized();

    SelectStatement select = this.sqlDialect.BuildGetListSql(schemaName, entityName, fields, filter, sort, limit, offset);
    using ActiveConnection connect = this.sqlDialect.GetConnection();
    var found = connect.Connection.Query(select.Sql, select.Parameters, connect.Transaction);
    return found.ToList();
  }

  public dynamic? GetResource(string schemaName, string entityName,
    string id, string fields = "", string include = "", DateTimeOffset? asOf = null)
  {
    // if (!authorization.GetPermissionToResource(schemaName, entityName, id, this.User).HasFlag(Permission.Get))
    //   return this.Unauthorized();

    SelectStatement select = this.sqlDialect.BuildGetSingleSql(schemaName, entityName, id, fields, include, asOf);
    using ActiveConnection connect = this.sqlDialect.GetConnection();
    var found = connect.Connection.QuerySingleOrDefault(select.Sql, select.Parameters, connect.Transaction);

    return found;
  }

  public dynamic PostResource(string schemaName, string entityName, JsonDocument jsonResourceRequestBody)
  {
    var entity = this.sqlDialect.FindEntity(schemaName, entityName);
    InsertStatement sql = this.sqlDialect.BuildPostSingleSql(schemaName, entityName, jsonResourceRequestBody);
    using ActiveConnection connect = this.sqlDialect.GetConnection();
    try
    {
      var found = connect.Connection.Query(sql.Sql, sql.Parameters, connect.Transaction);
      if (found.Count() < 1)
        throw new System.Data.InvalidExpressionException($"Insert Failed for {schemaName}:{entityName}");

      //TODO: Need to create Patchwork Log entry

      connect.Transaction.Commit();
      return found.First();
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine(ex.Message);
      connect.Transaction.Rollback();
      throw;
    }
  }

  public UpdatedResource PutResource(string schemaName, string entityName, string id, JsonDocument jsonResourceRequestBody)
  {
    SelectStatement select = this.sqlDialect.BuildGetSingleSql(schemaName, entityName, id);
    UpdateStatement update = this.sqlDialect.BuildPutSingleSql(schemaName, entityName, id, jsonResourceRequestBody);
    using ActiveConnection connect = this.sqlDialect.GetConnection();
    try
    {
      var beforeObject = connect.Connection.QuerySingleOrDefault(select.Sql, select.Parameters, connect.Transaction);
      if (beforeObject == null) throw new System.Data.RowNotInTableException();
      var beforeUpdate = JsonSerializer.Serialize(beforeObject);
      var updated = connect.Connection.Execute(update.Sql, update.Parameters, connect.Transaction);
      var afterObject = connect.Connection.QuerySingleOrDefault(select.Sql, select.Parameters, connect.Transaction);
      if (afterObject == null)
        throw new System.Data.DataException("Update Failed");
      var afterString = JsonSerializer.Serialize(afterObject);

      JsonPatch patch = this.sqlDialect.BuildDiffAsJsonPatch(beforeUpdate, afterString);

      //TODO: Append this patch to the Patchwork Log

      connect.Transaction.Commit();

      return new UpdatedResource(afterObject, patch);
    }
    catch (Exception)
    {
      connect.Transaction.Rollback();
      throw;
    }
  }

  public bool DeleteResource(string schemaName, string entityName, string id)
  {
    DeleteStatement delete = this.sqlDialect.BuildDeleteSingleSql(schemaName, entityName, id);
    using ActiveConnection connect = this.sqlDialect.GetConnection();
    try
    {
      var updated = connect.Connection.Execute(delete.Sql, delete.Parameters, connect.Transaction);
      if (updated < 1)
        throw new System.Data.RowNotInTableException();

      //TODO: Append this patch to the Patchwork Log

      connect.Transaction.Commit();

      return true;
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine(ex.Message);
      connect.Transaction.Rollback();
      return false;
    }
  }

public record UpdatedResource(dynamic Resource, JsonPatch changes);