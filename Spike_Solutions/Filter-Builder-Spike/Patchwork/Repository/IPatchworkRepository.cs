using System.Text.Json;
using Json.Patch;

namespace Patchwork.Repository;

public interface IPatchworkRepository
{
  GetListResult GetList(string schemaName, string entityName, string fields = "", string filter = "", string sort = "", int limit = 0, int offset = 0);
  GetResourceResult GetResource(string schemaName, string entityName, string id, string fields = "", string include = "", DateTimeOffset? asOf = null);
  PostResult PostResource(string schemaName, string entityName, JsonDocument jsonResourceRequestBody);
  PutResult PutResource(string schemaName, string entityName, string id, JsonDocument jsonResourceRequestBody);
  DeleteResult DeleteResource(string schemaName, string entityName, string id);
  PatchResourceResult PatchResource(string schemaName, string entityName, string id, JsonPatch jsonPatchRequestBody);
  PatchListResult PatchList(string schemaName, string entityName, JsonPatch jsonPatchRequestBody);
}
