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

    var select = this.sqlDialect.BuildGetListSql(schemaName, entityName, fields, filter, sort, limit, offset);
    using var connect = this.sqlDialect.GetConnection();
    var found = connect.Connection.Query(select.Sql, select.Parameters);
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

    var select = this.sqlDialect.BuildGetSingleSql(schemaName, entityName, id, fields, include, asOf);
    using var connect = this.sqlDialect.GetConnection();
    var found = connect.Connection.Query(select.Sql, select.Parameters);
    return Json(found);
  }

  [HttpPost]
  [Route("api/{schemaName}/{entityName}")]
  public IActionResult PostResourceEndpoint(
    [FromRoute] string schemaName,
    [FromRoute] string entityName,
    [FromBody] JsonDocument jsonResourceRequestBody)
  {
    // if (!authorization.GetPermissionToResource(schemaName, entityName, id, this.User).HasFlag(Permission.Post))
    //   return this.Unauthorized();

    InsertStatement sql = this.sqlDialect.BuildPostSingleSql(schemaName, entityName, jsonResourceRequestBody);

    using var connect = this.sqlDialect.GetConnection();
    var found = connect.Connection.Query(sql.Sql, sql.Parameters, connect.Transaction);

    connect.Transaction.Rollback();
    return Json(found);
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

    return Json(new { });
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

    return NoContent();
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
