using System.Text.Json;
using Dapper;
using Json.Patch;
using Microsoft.AspNetCore.Mvc;
using Patchwork.Authorization;
using Patchwork.SqlDialects;
using Patchwork.SqlStatements;

namespace Patchwork.Api.Controllers;

// [Authorize]
public class PatchworkEndpoints : Controller
{
  protected readonly IPatchworkAuthorization authorization;
  protected readonly ISqlDialectBuilder sqlDialect;

  public PatchworkEndpoints(IPatchworkAuthorization auth, ISqlDialectBuilder sql)
  {
    this.authorization = auth;
    this.sqlDialect = sql;
  }

  [HttpGet]
  [Route("api/{schemaName}/{entityName}")]
  public IActionResult GetListEndpoint(
    [FromRoute] string schemaName,
    [FromRoute] string entityName,
    [FromQuery] string fields = "",
    [FromQuery] string filter = "",
    [FromQuery] string sort = "",
    [FromQuery] int limit = 0,
    [FromQuery] int offset = 0)
  {
    // if (!authorization.GetPermissionToCollection(schemaName, entityName, this.User).HasFlag(Permission.Get))
    //   return this.Unauthorized();

    SelectStatement select = this.sqlDialect.BuildGetListSql(schemaName, entityName, fields, filter, sort, limit, offset);
    using ActiveConnection connect = this.sqlDialect.GetConnection();
    IEnumerable<dynamic> found = connect.Connection.Query(select.Sql, select.Parameters, connect.Transaction);
    return Json(found);
  }

  [HttpGet]
  [Route("api/{schemaName}/{entityName}/{id}")]
  public IActionResult GetResourceEndpoint(
    [FromRoute] string schemaName,
    [FromRoute] string entityName,
    [FromRoute] string id,
    [FromQuery] string fields = "",
    [FromQuery] string include = "",
    [FromQuery] DateTimeOffset? asOf = null)
  {
    // if (!authorization.GetPermissionToResource(schemaName, entityName, id, this.User).HasFlag(Permission.Get))
    //   return this.Unauthorized();

    SelectStatement select = this.sqlDialect.BuildGetSingleSql(schemaName, entityName, id, fields, include, asOf);
    using ActiveConnection connect = this.sqlDialect.GetConnection();
    var found = connect.Connection.QuerySingleOrDefault(select.Sql, select.Parameters, connect.Transaction);
    if (found == null)
      return NotFound();
    return Json(found);
  }

  [HttpPost]
  [Route("api/{schemaName}/{entityName}")]
  public IActionResult PostResourceEndpoint(
    [FromRoute] string schemaName,
    [FromRoute] string entityName,
    [FromBody] JsonDocument jsonResourceRequestBody)
  {
    // if (!authorization.GetPermissionToResource(schemaName, entityName, this.User).HasFlag(Permission.Post))
    //   return this.Unauthorized();

    InsertStatement sql = this.sqlDialect.BuildPostSingleSql(schemaName, entityName, jsonResourceRequestBody);
    using ActiveConnection connect = this.sqlDialect.GetConnection();
    try
    {
      var found = connect.Connection.Query(sql.Sql, sql.Parameters, connect.Transaction);

      //TODO: Need to create Patchwork Log entry

      connect.Transaction.Commit();
      return Json(found);
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine(ex.Message);
      connect.Transaction.Rollback();
      throw;
    }
  }

  [HttpPut]
  [Route("api/{schemaName}/{entityName}/{id}")]
  public IActionResult PutResourceEndpoint(
    [FromRoute] string schemaName,
    [FromRoute] string entityName,
    [FromRoute] string id,
    [FromBody] JsonDocument jsonResourceRequestBody)
  {
    // if (!authorization.GetPermissionToResource(schemaName, entityName, id, this.User).HasFlag(Permission.Put))
    //   return this.Unauthorized();

    SelectStatement select = this.sqlDialect.BuildGetSingleSql(schemaName, entityName, id);
    UpdateStatement update = this.sqlDialect.BuildPutSingleSql(schemaName, entityName, id, jsonResourceRequestBody);
    using ActiveConnection connect = this.sqlDialect.GetConnection();
    try
    {

      var beforeObject = connect.Connection.QuerySingleOrDefault(select.Sql, select.Parameters, connect.Transaction);
      if (beforeObject == null)
        return NotFound();
      var beforeUpdate = JsonSerializer.Serialize(beforeObject);
      var updated = connect.Connection.Execute(update.Sql, update.Parameters, connect.Transaction);
      var afterObject = connect.Connection.QuerySingleOrDefault(select.Sql, select.Parameters, connect.Transaction);
      if (afterObject == null)
        return BadRequest($"Failed to insert the object {jsonResourceRequestBody}");
      var afterString = JsonSerializer.Serialize(afterObject);

      JsonPatch patch = this.sqlDialect.BuildDiffAsJsonPatch(beforeUpdate, afterString);

      //TODO: Append this patch to the Patchwork Log

      connect.Transaction.Commit();

      return Json(new { entity = afterObject, changes = patch });
    }
    catch (Exception)
    {
      connect.Transaction.Rollback();
      throw;
    }
  }

  [HttpDelete]
  [Route("api/{schemaName}/{entityName}/{id}")]
  public IActionResult DeleteResourceEndpoint(
    [FromRoute] string schemaName,
    [FromRoute] string entityName,
    [FromRoute] string id)
  {
    // if (!authorization.GetPermissionToResource(schemaName, entityName, id, this.User).HasFlag(Permission.Delete))
    //   return this.Unauthorized();

    var delete = this.sqlDialect.BuildDeleteSingleSql(schemaName, entityName, id);
    using ActiveConnection connect = this.sqlDialect.GetConnection();
    try
    {

      var updated = connect.Connection.Execute(delete.Sql, delete.Parameters, connect.Transaction);

      //TODO: Need to create Patchwork Log entry

      connect.Transaction.Commit();

      return NoContent();
    }
    catch (Exception)
    {
      connect.Transaction.Rollback();
      throw;
    }

  }

  [HttpPatch]
  [Route("api/{schemaName}/{entityName}")]
  public IActionResult PatchListEndpoint(
    [FromRoute] string schemaName,
    [FromRoute] string entityName,
    [FromBody] JsonPatch jsonPatchRequestBody)
  {
    // if (!authorization.GetPermissionToCollection(schemaName, entityName, this.User).HasFlag(Permission.Patch))
    //   return this.Unauthorized();

    //TODO: The incoming JSON Patch will have opertions for one or more entities in the system. We identify which
    //      entities are being changed by the `path` element. The prefix on each `path` element will be the URL needed
    //      to access an entity's GET RESOURCE endpoint and the suffix will indicate which elements inside the entity
    //      are being modified. To apply a group JSON Patch, we use this procedure:
    //
    // 1. Read the incoming JSON Patch and group all operations by the first parameters in the `path` element.
    // 2. Create a new JSON Patch object for each entity being modified.
    // 3. Remove the prefix of `schema/entity/id` from each operation as it is added to the new JSON Patch document.
    // 4. Add the opertion with the shortned `path` to the matching element in new list of JSON Patch documents.
    //    4.1 Some operations target existing records for modification and have a suffix for the element to modify,
    //        These operations will be used to perform UPDATE statements against the database.
    //    4.2 Some operations will insert a new record and they have an id of `-`. These operations will be used 
    //        to perform INSERT statements against the database.
    //    4.3 Some `remove` operations will target an entity by have no suffix. In this case, the system will create
    //        a DELETE statement to remove the entity from the system.
    // 5. Begin a database transaction.
    // 6. For each JSON Patch in the new list:
    //    6.1 Read the entity from the database that matches the `PATH` prefix
    //    6.2 Apply the JSON Patch to the object
    //    6.3 Generate a PUT statement for the new object version
    //    6.4 Execute the PUT statement against the database
    //    6.5 Save the JSON Patch into the event log
    // 7. If all changes succeed without error, then we can commit the transaction. But, if any operations fail, then
    //    we MUST rollback all changes. The entire JSON Patch list must succeed or the entire list must fail.

    return Accepted();
  }

  [HttpPatch]
  [Route("api/{schemaName}/{entityName}/{id}")]
  public IActionResult PatchResourceEndpoint(
    [FromRoute] string schemaName,
    [FromRoute] string entityName,
    [FromRoute] string id,
    [FromBody] JsonPatch jsonPatchRequestBody)
  {
    // if (!authorization.GetPermissionToResource(schemaName, entityName, id, this.User).HasFlag(Permission.Patch))
    //   return this.Unauthorized();

    //TODO:
    // 1. Read current entity
    // 2. Apply patch document to entity
    // 3. Perform a PUT with the updated entity
    // 4. Return updated entity as `entity` and Patch as `changes` like the PUT does.

    return Accepted();
  }
}
