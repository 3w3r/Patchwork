using System.Text.Json;
using Json.Patch;
using Microsoft.AspNetCore.Mvc;
using Patchwork.Authorization;
using Patchwork.Repository;
using Patchwork.SqlDialects;

namespace Patchwork.Controllers;

public abstract class PatchworkControllerBase : Controller
{
  protected readonly IPatchworkAuthorization authorization;
  protected readonly ISqlDialectBuilder sqlDialect;
  protected readonly IPatchworkRepository Repository;

  public PatchworkControllerBase(IPatchworkRepository repo, IPatchworkAuthorization auth, ISqlDialectBuilder sql)
  {
    this.Repository = repo;
    this.authorization = auth;
    this.sqlDialect = sql;
  }

  protected IActionResult GetListEndpoint(string schemaName, int version, string entityName,
    string fields = "", string filter = "", string sort = "", int limit = 0, int offset = 0)
  {
    if (limit == 0) 
      limit=this.Request.Headers.GetLimitFromRangeHeader();
    if (offset == 0)
      offset=this.Request.Headers.GetOffsetFromRangeHeader();

    GetListResult found = Repository.GetList(schemaName, entityName, fields, filter, sort, limit, offset);
    this.Response.Headers.AddContentRangeHeader(found);

    return Json(found.Resources);
  }

  protected IActionResult GetResourceEndpoint(string schemaName, int version, string entityName, string id,
    string fields = "", string include = "", DateTimeOffset? asOf = null)
  {
    if (asOf.HasValue)
    {
      // NOTE: Rebuilding a resource from the log to a specific point in time is much more expensive than
      //       just getting the current version of the entity. Also, includes and field projections are 
      //       not available when querying older versions of entities.
      GetResourceAsOfResult found = Repository.GetResourceAsOf(schemaName, entityName, id, asOf.Value);
      this.Response.Headers.AddDateAndRevisionHeader(found);

      if (found.Resource == null)
        return NotFound();
      return Json(found.Resource);
    }
    else
    {
      GetResourceResult found = Repository.GetResource(schemaName, entityName, id, fields, include);

      if (found.Resource == null)
        return NotFound();
      return Json(found.Resource);
    }
  }

  protected IActionResult PostResourceEndpoint(string schemaName, int version, string entityName, JsonDocument jsonResourceRequestBody)
  {
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

  protected IActionResult PutResourceEndpoint(string schemaName, int version, string entityName, string id, JsonDocument jsonResourceRequestBody)
  {
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

  protected IActionResult DeleteResourceEndpoint(string schemaName, int version, string entityName, string id)
  {
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

  protected IActionResult PatchListEndpoint(string schemaName, int version, string entityName, JsonPatch jsonPatchRequestBody)
  {
    //TODO: The incoming JSON Patch will have opertions for one or more entities in the system. We identify which
    //      entities are being changed by the `path` element. The prefix on each path element will be the URL needed
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

    throw new NotImplementedException();
  }

  protected IActionResult PatchResourceEndpoint(string schemaName, int version, string entityName, string id, JsonPatch jsonPatchRequestBody)
  {
    PatchResourceResult result = this.Repository.PatchResource(schemaName, entityName, id, jsonPatchRequestBody);
    this.Response.Headers.AddPatchChangesHeader(result.Changes);

    return this.Json(result.Resource);
  }
}