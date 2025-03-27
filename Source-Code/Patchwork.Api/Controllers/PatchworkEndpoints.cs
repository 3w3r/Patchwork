using System.Text.Json;
using Json.Patch;
using Microsoft.AspNetCore.Mvc;
using Patchwork.Authorization;
using Patchwork.Repository;
using Patchwork.SqlDialects;
using Patchwork.Controllers;

namespace Patchwork.Api.Controllers;

[Route("repo/{schemaName}/v{version}/{entityName}")]
public class PatchworkEndpoints : Controller
{
  protected readonly IPatchworkAuthorization authorization;
  protected readonly ISqlDialectBuilder sqlDialect;
  protected readonly IPatchworkRepository Repository;

  public PatchworkEndpoints(IPatchworkRepository repo, IPatchworkAuthorization auth, ISqlDialectBuilder sql)
  {
    this.Repository = repo;
    this.authorization = auth;
    this.sqlDialect = sql;
  }

  [HttpGet]
  public IActionResult GetListEndpoint(
    [FromRoute] string schemaName,
    [FromRoute] int version,
    [FromRoute] string entityName,
    [FromQuery] string fields = "",
    [FromQuery] string filter = "",
    [FromQuery] string sort = "",
    [FromQuery] int limit = 0,
    [FromQuery] int offset = 0)
  {
    // if (!authorization.GetPermissionToCollection(schemaName, entityName, this.User).HasFlag(Permission.Get)) return this.Unauthorized();
    schemaName = NormalizeSchemaName(schemaName);

    GetListResult found = Repository.GetList(schemaName, entityName, fields, filter, sort, limit, offset);
    this.Response.Headers.AddContentRangeHeader(found);

    return Json(found.Resources);
  }

  [HttpGet]
  [Route("{id}")]
  public IActionResult GetResourceEndpoint(
    [FromRoute] string schemaName,
    [FromRoute] int version,
    [FromRoute] string entityName,
    [FromRoute] string id,
    [FromQuery] string fields = "",
    [FromQuery] string include = "",
    [FromQuery] DateTimeOffset? asOf = null)
  {
    // if (!authorization.GetPermissionToResource(schemaName, entityName, id, this.User).HasFlag(Permission.Get)) return this.Unauthorized();
    schemaName = NormalizeSchemaName(schemaName);
    if (asOf == null)
    {
      GetResourceResult found = Repository.GetResource(schemaName, entityName, id, fields, include);

      if (found.Resource == null)
        return NotFound();
      return Json(found.Resource);

    }
    else
    {
      GetResourceAsOfResult found = Repository.GetResourceAsOf(schemaName, entityName, id, asOf.Value);

      if (found.Resource == null)
        return NotFound();
      return Json(found.Resource);
    }
  }

  [HttpPost]
  public IActionResult PostResourceEndpoint(
    [FromRoute] string schemaName,
    [FromRoute] int version,
    [FromRoute] string entityName,
    [FromBody] JsonDocument jsonResourceRequestBody)
  {
    // if (!authorization.GetPermissionToResource(schemaName, entityName, this.User).HasFlag(Permission.Post)) return this.Unauthorized();
    schemaName = NormalizeSchemaName(schemaName);
    try
    {
      PostResult found = Repository.PostResource(schemaName, entityName, jsonResourceRequestBody);

      return Created($"api/{schemaName}/v{version}/{entityName}/{found.Id}", found.Resource);
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine(ex.Message);
      return BadRequest(ex.Message);
    }
  }

  [HttpPut]
  [Route("{id}")]
  public IActionResult PutResourceEndpoint(
    [FromRoute] string schemaName,
    [FromRoute] int version,
    [FromRoute] string entityName,
    [FromRoute] string id,
    [FromBody] JsonDocument jsonResourceRequestBody)
  {
    // if (!authorization.GetPermissionToResource(schemaName, entityName, id, this.User).HasFlag(Permission.Put)) return this.Unauthorized();
    schemaName = NormalizeSchemaName(schemaName);
    try
    {
      PutResult updated = this.Repository.PutResource(schemaName, entityName, id, jsonResourceRequestBody);
      this.Response.Headers.AddPatchChangesHeader(updated.Changes);
      return Json(updated.Resource);
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine(ex.Message);
      return BadRequest(ex.Message);
    }
  }

  [HttpDelete]
  [Route("{id}")]
  public IActionResult DeleteResourceEndpoint(
    [FromRoute] string schemaName,
    [FromRoute] int version,
    [FromRoute] string entityName,
    [FromRoute] string id)
  {
    // if (!authorization.GetPermissionToResource(schemaName, entityName, id, this.User).HasFlag(Permission.Delete)) return this.Unauthorized();
    schemaName = NormalizeSchemaName(schemaName);
    try
    {
      DeleteResult successfulDelete = this.Repository.DeleteResource(schemaName, entityName, id);
      if (successfulDelete.Success)
        return NoContent();
      else
        return BadRequest();
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine(ex.Message);
      return BadRequest(ex.Message);
    }
  }

  [HttpPatch]
  public IActionResult PatchListEndpoint(
    [FromRoute] string schemaName,
    [FromRoute] int version,
    [FromRoute] string entityName,
    [FromBody] JsonPatch jsonPatchRequestBody)
  {
    // if (!authorization.GetPermissionToCollection(schemaName, entityName, this.User).HasFlag(Permission.Patch)) return this.Unauthorized();
    schemaName = NormalizeSchemaName(schemaName);
    try
    {
      PatchListResult result = this.Repository.PatchList(schemaName, entityName, jsonPatchRequestBody);
      //Add header w/ changes
      return this.Json(result);
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine(ex.Message);
      return BadRequest(ex.Message);
    }
  }

  [HttpPatch]
  [Route("{id}")]
  public IActionResult PatchResourceEndpoint(
    [FromRoute] string schemaName,
    [FromRoute] int version,
    [FromRoute] string entityName,
    [FromRoute] string id,
    [FromBody] JsonPatch jsonPatchRequestBody)
  {
    // if (!authorization.GetPermissionToResource(schemaName, entityName, id, this.User).HasFlag(Permission.Patch)) return this.Unauthorized();
    schemaName = NormalizeSchemaName(schemaName);

    PatchResourceResult result = this.Repository.PatchResource(schemaName, entityName, id, jsonPatchRequestBody);
    this.Response.Headers.AddPatchChangesHeader(result.Changes);

    return this.Json(result.Resource);

  }

  private string NormalizeSchemaName(string schemaName)
  {
    if (schemaName.Equals("surveys", StringComparison.OrdinalIgnoreCase))
      schemaName = sqlDialect.DefaultSchemaName;
    return schemaName;
  }
}
