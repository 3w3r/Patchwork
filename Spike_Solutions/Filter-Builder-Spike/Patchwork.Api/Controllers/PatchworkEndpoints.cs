using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using Patchwork.Authorization;
using Patchwork.SqlDialects;
using Dapper;
using DatabaseSchemaReader.Filters;
using System.Collections.Generic;

namespace Patchwork.Api.Controllers;

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
    var select = this.sqlDialect.BuildGetListSql(schemaName, entityName, fields, filter, sort, limit, offset);
    using DbConnection connect = this.sqlDialect.GetConnection();
    connect.Open();
    var found = connect.Query(select.Sql, select.Parameters);
    connect.Close();
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
    var select = this.sqlDialect.BuildGetSingleSql(schemaName, entityName, id, fields, include, asOf);
    using DbConnection connect = this.sqlDialect.GetConnection();
    connect.Open();
    var found = connect.Query(select.Sql, select.Parameters);
    connect.Close();
    return Json(found);
  }

  [HttpPost]
  [Route("api/{schemaName}/{entityName}/{id}")]
  public IActionResult PostResourceEndpoint(
    [FromRoute] string schemaName,
    [FromRoute] string entityName,
    [FromBody] string jsonResourceRequestBody)
  {
    return View();
  }

  [HttpPut]
  [Route("api/{schemaName}/{entityName}/{id}")]
  public IActionResult PutResourceEndpoint(
    [FromRoute] string schemaName,
    [FromRoute] string entityName,
    [FromRoute] string id,
    [FromBody] string jsonResourceRequestBody)
  {
    return View();
  }

  [HttpDelete]
  [Route("api/{schemaName}/{entityName}/{id}")]
  public IActionResult DeleteResourceEndpoint(
    [FromRoute] string schemaName,
    [FromRoute] string entityName,
    [FromRoute] string id)
  {
    return View();
  }

  [HttpPatch]
  [Route("api/{schemaName}/{entityName}")]
  public IActionResult PatchListEndpoint(
    [FromRoute] string schemaName,
    [FromRoute] string entityName,
    [FromBody] string jsonPatchRequestBody)
  {
    return View();
  }
}
