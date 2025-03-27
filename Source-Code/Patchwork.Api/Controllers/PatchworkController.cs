using System.Text.Json;
using Json.Patch;
using Microsoft.AspNetCore.Mvc;
using Patchwork.Authorization;
using Patchwork.Controllers;
using Patchwork.Repository;
using Patchwork.SqlDialects;

namespace Patchwork.Api.Controllers
{
  /// <summary>
  ///   The PatchworkController class is responsible for handling HTTP requests related to Patchwork operations. It
  ///   provides endpoints for performing CRUD (Create, Read, Update, Delete) operations on resources within a
  ///   specified schema and entity. The PatchworkController class also supports JSON Patch operations for partial
  ///   updates.
  /// </summary>
  [Route("api/{schemaName}/v1/{entityName}")]
  public class PatchworkController : PatchworkControllerBase
  {
    protected ILogger Log { get; }
    protected const int Version = 1;

    /// <summary>
    ///   Initializes a new instance of the PatchworkController class.
    /// </summary>
    /// <param name="logger">An instance of ILogger for logging events and errors.</param>
    /// <param name="repo">An instance of IPatchworkRepository for interacting with the data repository.</param>
    /// <param name="auth">An instance of IPatchworkAuthorization for managing authorization rules.</param>
    /// <param name="sql">An instance of ISqlDialectBuilder for building SQL queries.</param>
    public PatchworkController(
      ILogger<PatchworkController> logger,
      IPatchworkRepository repo,
      IPatchworkAuthorization auth,
      ISqlDialectBuilder sql) : base(repo, auth, sql)
    {
      Log = logger;
    }

    /// <summary>
    ///   Retrieves a list of resources based on the provided parameters.
    /// </summary>
    /// <param name="schemaName">The name of the schema.</param>
    /// <param name="entityName">The name of the entity.</param>
    /// <param name="fields">A comma-separated list of fields to include in the response.</param>
    /// <param name="filter">A filter expression to apply to the result set.</param>
    /// <param name="sort">A comma-separated list of fields to sort the result set by.</param>
    /// <param name="limit">The maximum number of records to return.</param>
    /// <param name="offset">The number of records to skip before returning results.</param>
    /// <returns>
    ///   An `IActionResult` containing the list of resources or an Unauthorized result if the user is not
    ///   authenticated or a client.
    /// </returns>
    [HttpGet]
    public IActionResult GetList(
      [FromRoute] string schemaName,
      [FromRoute] string entityName,
      [FromQuery] string fields = "",
      [FromQuery] string filter = "",
      [FromQuery] string sort = "",
      [FromQuery] int limit = 0,
      [FromQuery] int offset = 0
      )
    {
      schemaName = NormalizeSchemaName(schemaName);

      IActionResult result = GetListEndpoint(schemaName, Version, entityName, fields, filter, sort, limit, offset);
      return result;
    }

    /// <summary>
    ///   Retrieves a specific resource based on the provided parameters.
    /// </summary>
    /// <param name="schemaName">The name of the schema. If 'surveys' is provided, it will be replaced with the default schema name.</param>
    /// <param name="entityName">The name of the entity.</param>
    /// <param name="id">The unique identifier of the resource to retrieve.</param>
    /// <param name="fields">A comma-separated list of fields to include in the response. If not provided, all fields will be returned.</param>
    /// <param name="include">A comma-separated list of related resources to include in the response. If not provided, no related resources will be returned.</param>
    /// <param name="asOf">A timestamp indicating the point in time from which to retrieve the resource. If not provided, the current state of the resource will be returned.</param>
    /// <returns>An IActionResult containing the requested resource or an Unauthorized result if the user is not authenticated or a client.</returns>
    [HttpGet, Route("{id}")]
    public IActionResult GetResource(
      [FromRoute] string schemaName,
      [FromRoute] string entityName,
      [FromRoute] string id,
      [FromQuery] string fields = "",
      [FromQuery] string include = "",
      [FromQuery] DateTimeOffset? asOf = null
      )
    {
      schemaName = NormalizeSchemaName(schemaName);

      IActionResult result = GetResourceEndpoint(schemaName, Version, entityName, id, fields, include, asOf);
      return result;
    }

    /// <summary>
    ///   Posts a new resource to the specified entity within the given schema.
    /// </summary>
    /// <param name="schemaName">The name of the schema. If 'surveys' is provided, it will be replaced with the default schema name.</param>
    /// <param name="entityName">The name of the entity.</param>
    /// <param name="jsonResourceRequestBody">The JSON representation of the resource to be created.</param>
    /// <returns>An IActionResult containing the created resource or an Unauthorized result if the user is not authenticated or a client.</returns>
    [HttpPost]
    public IActionResult Resource(
      [FromRoute] string schemaName,
      [FromRoute] string entityName,
      JsonDocument jsonResourceRequestBody
      )
    {
      schemaName = NormalizeSchemaName(schemaName);

      IActionResult result = PostResourceEndpoint(schemaName, Version, entityName, jsonResourceRequestBody);
      return result;
    }

    /// <summary>
    ///   Updates an existing resource in the specified schema and entity.
    /// </summary>
    /// <param name="schemaName">The name of the schema.</param>
    /// <param name="entityName">The name of the entity.</param>
    /// <param name="id">The unique identifier of the resource to update.</param>
    /// <param name="jsonResourceRequestBody">The JSON representation of the updated resource.</param>
    /// <returns>An IActionResult indicating the outcome of the operation.</returns>
    [HttpPut, Route("{id}")]
    public IActionResult PutResource(
      [FromRoute] string schemaName,
      [FromRoute] string entityName,
      [FromRoute] string id,
      JsonDocument jsonResourceRequestBody
      )
    {
      schemaName = NormalizeSchemaName(schemaName);

      IActionResult result = PutResourceEndpoint(schemaName, Version, entityName, id, jsonResourceRequestBody);
      return result;
    }

    /// <summary>
    ///   Deletes a resource from the specified schema and entity.
    /// </summary>
    /// <param name="schemaName">The name of the schema.</param>
    /// <param name="entityName">The name of the entity.</param>
    /// <param name="id">The unique identifier of the resource to delete.</param>
    /// <returns>An IActionResult indicating the outcome of the operation.</returns>
    [HttpDelete, Route("{id}")]
    public IActionResult DeleteResource(
      string schemaName, string entityName, string id
      )
    {
      schemaName = NormalizeSchemaName(schemaName);

      IActionResult result = DeleteResourceEndpoint(schemaName, Version, entityName, id);
      return result;
    }

    /// <summary>
    ///   Applies a JSON Patch operation to a list of resources in the specified schema and entity.
    /// </summary>
    /// <param name="schemaName">The name of the schema.</param>
    /// <param name="entityName">The name of the entity.</param>
    /// <param name="jsonPatchRequestBody">The JSON Patch document containing the operations to apply.</param>
    /// <returns>An IActionResult indicating the outcome of the operation.</returns>
    [HttpPatch]
    public IActionResult PatchList(
      [FromRoute] string schemaName,
      [FromRoute] string entityName,
      JsonPatch jsonPatchRequestBody
      )
    {
      schemaName = NormalizeSchemaName(schemaName);

      IActionResult result = PatchListEndpoint(schemaName, Version, entityName, jsonPatchRequestBody);
      return result;
    }

    /// <summary>
    ///   Applies a JSON Patch operation to a specific resource in the specified schema and entity.
    /// </summary>
    /// <param name="schemaName">The name of the schema.</param>
    /// <param name="entityName">The name of the entity.</param>
    /// <param name="id">The unique identifier of the resource to update.</param>
    /// <param name="jsonPatchRequestBody">The JSON Patch document containing the operations to apply.</param>
    /// <returns>An IActionResult indicating the outcome of the operation.</returns>
    [HttpPatch, Route("{id}")]
    public IActionResult PatchResource(
      [FromRoute] string schemaName,
      [FromRoute] string entityName,
      [FromRoute] string id,
      JsonPatch jsonPatchRequestBody
      )
    {
      schemaName = NormalizeSchemaName(schemaName);

      IActionResult result = PatchResourceEndpoint(schemaName, Version, entityName, id, jsonPatchRequestBody);
      return result;
    }

    private string NormalizeSchemaName(string schemaName)
    {
      if (schemaName.Equals("surveys", StringComparison.OrdinalIgnoreCase))
        schemaName = sqlDialect.DefaultSchemaName;
      return schemaName;
    }
  }
}
