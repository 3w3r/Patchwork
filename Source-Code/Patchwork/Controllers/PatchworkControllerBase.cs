using System.Text.Json;
using Json.Patch;
using Microsoft.AspNetCore.Http;
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

  protected PatchworkControllerBase(IPatchworkRepository repo, IPatchworkAuthorization auth, ISqlDialectBuilder sql)
  {
    this.Repository = repo;
    this.authorization = auth;
    this.sqlDialect = sql;
  }

  /// <summary>
  /// Retrieves a list of resources based on the provided parameters.
  /// </summary>
  /// <param name="schemaName">The name of the schema.</param>
  /// <param name="version">The version of the API.</param>
  /// <param name="entityName">The name of the entity.</param>
  /// <param name="fields">The fields to include in the response.</param>
  /// <param name="filter">The filter to apply to the results.</param>
  /// <param name="sort">The sorting criteria for the results.</param>
  /// <param name="limit">The maximum number of results to return.</param>
  /// <param name="offset">The number of results to skip.</param>
  /// <returns>An ActionResult containing the list of resources.</returns>
  protected IActionResult GetListEndpoint(string schemaName, int version, string entityName,
      string fields = "", string filter = "", string sort = "", int limit = 0, int offset = 0)
  {
    // If the limit is not provided, retrieve it from the Range header
    if (limit == 0)
      limit = this.Request.Headers.GetLimitFromRangeHeader();

    // If the offset is not provided, retrieve it from the Range header
    if (offset == 0)
      offset = this.Request.Headers.GetOffsetFromRangeHeader();

    // Retrieve the list of resources from the repository
    GetListResult found = Repository.GetList(schemaName, entityName, fields, filter, sort, limit, offset);

    // Add the Content-Range header to the response
    this.Response.Headers.AddContentRangeHeader(found);

    // Return the list of resources as a JSON response
    return Json(found.Resources);
  }

  /// <summary>
  /// Retrieves a specific resource based on the provided parameters.
  /// </summary>
  /// <param name="schemaName">The name of the schema.</param>
  /// <param name="version">The version of the API.</param>
  /// <param name="entityName">The name of the entity.</param>
  /// <param name="id">The unique identifier of the resource.</param>
  /// <param name="fields">The fields to include in the response.</param>
  /// <param name="include">The related resources to include in the response.</param>
  /// <param name="asOf">The specific point in time to retrieve the resource.</param>
  /// <returns>An ActionResult containing the specific resource.</returns>
  protected IActionResult GetResourceEndpoint(string schemaName, int version, string entityName, string id,
      string fields = "", string include = "", DateTimeOffset? asOf = null)
  {
    // If the 'asOf' parameter is provided, retrieve the resource as it existed at the specified point in time.
    // This is useful for historical data analysis or auditing purposes.
    if (asOf.HasValue)
    {
      // NOTE: Rebuilding a resource from the log to a specific point in time is much more expensive than
      //       just getting the current version of the entity. Also, includes and field projections are 
      //       not available when querying older versions of entities.
      GetResourceAsOfResult found = Repository.GetResourceAsOf(schemaName, entityName, id, asOf.Value);
      this.Response.Headers.AddDateAndRevisionHeader(found);

      // If the resource was not found at the specified point in time, return a 'Not Found' response.
      if (found.Resource == null)
        return NotFound();
      // Otherwise, return the retrieved resource.
      return Json(found.Resource);
    }
    // If the 'asOf' parameter is not provided, retrieve the current version of the resource.
    else
    {
      // Retrieve the resource using the provided parameters.
      GetResourceResult found = Repository.GetResource(schemaName, entityName, id, fields, include);

      // If the resource was not found, return a 'Not Found' response.
      if (found.Resource == null)
        return NotFound();
      // Otherwise, return the retrieved resource.
      return Json(found.Resource);
    }
  }

  /// <summary>
  /// Posts a new resource to the specified schema and entity.
  /// </summary>
  /// <param name="schemaName">The name of the schema.</param>
  /// <param name="version">The version of the schema.</param>
  /// <param name="entityName">The name of the entity.</param>
  /// <param name="jsonResourceRequestBody">The JSON representation of the resource to be posted.</param>
  /// <returns>A Created response with the URI of the newly created resource and its JSON representation.</returns>
  /// <exception cref="Exception">An exception that occurred during the post operation.</exception>
  protected IActionResult PostResourceEndpoint(string schemaName, int version, string entityName, JsonDocument jsonResourceRequestBody)
  {
    try
    {
      // Attempt to post the resource to the repository
      PostResult found = Repository.PostResource(schemaName, entityName, jsonResourceRequestBody);

      // If successful, return a Created response with the URI of the newly created resource and its JSON representation
      return Created($"api/{schemaName}/v{version}/{entityName}/{found.Id}", found.Resource);
    }
    catch (Exception ex)
    {
      // If an exception occurs, log the error message and return a BadRequest response with the error message
      System.Diagnostics.Debug.WriteLine(ex.Message);
      return BadRequest(ex.Message);
    }
  }

  /// <summary>
  /// Updates an existing resource in the specified schema and entity.
  /// </summary>
  /// <param name="schemaName">The name of the schema.</param>
  /// <param name="version">The version of the schema.</param>
  /// <param name="entityName">The name of the entity.</param>
  /// <param name="id">The unique identifier of the resource to be updated.</param>
  /// <param name="jsonResourceRequestBody">The JSON representation of the updated resource.</param>
  /// <returns>A JSON response with the updated resource and a Patch-Changes header indicating the changes made.</returns>
  /// <exception cref="Exception">An exception that occurred during the update operation.</exception>
  protected IActionResult PutResourceEndpoint(string schemaName, int version, string entityName, string id, JsonDocument jsonResourceRequestBody)
  {
    try
    {
      // Attempt to update the resource in the repository
      PutResult updated = this.Repository.PutResource(schemaName, entityName, id, jsonResourceRequestBody);

      // Add a Patch-Changes header to the response indicating the changes made
      this.Response.Headers.AddPatchChangesHeader(updated.Changes);

      // Return a JSON response with the updated resource
      return Json(updated.Resource);
    }
    catch (Exception ex)
    {
      // If an exception occurs, log the error message and return a BadRequest response with the error message
      System.Diagnostics.Debug.WriteLine(ex.Message);
      return BadRequest(ex.Message);
    }
  }

  /// <summary>
  /// Deletes a resource based on the provided schema name, version, entity name, and ID.
  /// </summary>
  /// <param name="schemaName">The name of the schema.</param>
  /// <param name="version">The version of the schema.</param>
  /// <param name="entityName">The name of the entity.</param>
  /// <param name="id">The ID of the resource to delete.</param>
  /// <returns>
  /// Returns HTTP 204 No Content if the resource is successfully deleted.
  /// Returns HTTP 400 Bad Request if an error occurs during deletion.
  /// </returns>
  protected IActionResult DeleteResourceEndpoint(string schemaName, int version, string entityName, string id)
  {
    try
    {
      // Attempt to delete the resource
      DeleteResult successfulDelete = this.Repository.DeleteResource(schemaName, entityName, id);

      // If the deletion is successful, return HTTP 204 No Content
      if (successfulDelete.Success)
      {
        return NoContent();
      }
      else
      {
        // If the deletion fails, return HTTP 400 Bad Request
        return BadRequest();
      }
    }
    catch (Exception ex)
    {
      // Log the exception message for debugging purposes
      System.Diagnostics.Debug.WriteLine(ex.Message);

      // Return HTTP 400 Bad Request with the exception message
      return BadRequest(ex.Message);
    }
  }

  /// <summary>
  /// Performs a patch operation on a list of resources based on the provided schema name, version, entity name, and JSON Patch request body.
  /// </summary>
  /// <param name="schemaName">The name of the schema.</param>
  /// <param name="version">The version of the schema.</param>
  /// <param name="entityName">The name of the entity.</param>
  /// <param name="jsonPatchRequestBody">The JSON Patch request body.</param>
  /// <returns>
  /// Returns a custom multi-status response with HTTP 207 Multi-Status status code.
  /// The response contains details about the created, updated, and deleted resources.
  /// </returns>
  protected IActionResult PatchListEndpoint(string schemaName, int version, string entityName, JsonPatch jsonPatchRequestBody)
  {
    PatchListResult result = this.Repository.PatchList(schemaName, entityName, jsonPatchRequestBody);

    // HTTP 207: Multi-Status is the correct code to return when multiple updates that have different statuses have occurred.
    // Create a custom response with a 207 status code

    // Extract the details about the created, updated, and deleted resources from the result
    var created = result.Created.Select(x => new { id = x.Id, href = $"{this.Request.Path}/{x.Id}", status = 201, description = "Created" });
    var updated = result.Updated.Select(x => new { id = x.Id, href = $"{this.Request.Path}/{x.Id}", status = 200, description = "Updated" });
    var deleted = result.Deleted.Select(x => new { id = x.Id, href = $"{this.Request.Path}/{x.Id}", status = 204, description = "Deleted" });

    // Combine all the results into a single multi-status response
    return new ObjectResult(created.Concat(updated).Concat(deleted).ToList())
    {
      StatusCode = StatusCodes.Status207MultiStatus
    };
  }

  /// <summary>
  /// Performs a PATCH operation on a specific resource identified by its ID.
  /// </summary>
  /// <param name="schemaName">The name of the schema.</param>
  /// <param name="version">The version of the schema.</param>
  /// <param name="entityName">The name of the entity.</param>
  /// <param name="id">The ID of the resource to be patched.</param>
  /// <param name="jsonPatchRequestBody">The JSON Patch request body.</param>
  /// <returns>An IActionResult containing the patched resource.</returns>
  protected IActionResult PatchResourceEndpoint(string schemaName, int version, string entityName, string id, JsonPatch jsonPatchRequestBody)
  {
    // Call the PatchResource method from the Repository to perform the PATCH operation
    PatchResourceResult result = this.Repository.PatchResource(schemaName, entityName, id, jsonPatchRequestBody);

    // Add the Patch-Changes header to the response with the changes made during the PATCH operation
    this.Response.Headers.AddPatchChangesHeader(result.Changes);

    // Return the patched resource as a JSON response
    return this.Json(result.Resource);
  }
}