using Json.Patch;
using Patchwork.DbSchema;
using Patchwork.SqlStatements;

namespace Patchwork.SqlDialects;

public interface ISqlDialectBuilder
{
  DatabaseMetadata DiscoverSchema();

  SelectStatement BuildGetListSql(string schemaName, string entityName, string fields = "", string filter = "", string sort = "", int limit = 0, int offset = 0);
  SelectStatement BuildGetSingleSql(string schemaName, string entityName, string id, string fields = "", string include = "", DateTimeOffset? asOf = null);

  PatchStatement BuildPatchListSql(string schemaName, string entityName, JsonPatch jsonPatchRequestBody);
  UpdateStatement BuildPutSingleSql(string schemaName, string entityName, string id, string jsonResourceRequestBody);
  PatchStatement BuildPatchSingleSql(string schemaName, string entityName, string id, JsonPatch jsonPatchRequestBody);
  DeleteStatement BuildDeleteSingleSql(string schemaName, string entityName, string id);

}
