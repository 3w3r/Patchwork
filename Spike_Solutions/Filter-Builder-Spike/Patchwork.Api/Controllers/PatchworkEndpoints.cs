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
      var found = connect.Connection.Execute(sql.Sql, sql.Parameters, connect.Transaction);

      //TODO: Need to create Patchwork Log entry

      connect.Transaction.Commit();
      return Json(found);
    }
    catch (Exception)
    {
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

    return Accepted();
  }
}
