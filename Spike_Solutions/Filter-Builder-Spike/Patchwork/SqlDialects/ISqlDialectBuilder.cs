using System.Data.Common;
using Json.Patch;
using Patchwork.DbSchema;
using Patchwork.SqlStatements;

namespace Patchwork.SqlDialects;

public interface ISqlDialectBuilder
{
  DbConnection GetConnection();
  DatabaseMetadata DiscoverSchema();

  SelectStatement BuildGetListSql(string schemaName, string entityName, string fields = "", string filter = "", string sort = "", int limit = 0, int offset = 0);
  SelectStatement BuildGetSingleSql(string schemaName, string entityName, string id, string fields = "", string include = "", DateTimeOffset? asOf = null);

  InsertStatement BuildPostSingleSql(string schemaName, string entityName, string jsonResourceRequestBody);
  UpdateStatement BuildPutSingleSql(string schemaName, string entityName, string id, string jsonResourceRequestBody);
  DeleteStatement BuildDeleteSingleSql(string schemaName, string entityName, string id);

  PatchStatement BuildPatchListSql(string schemaName, string entityName, JsonPatch jsonPatchRequestBody);
  PatchStatement BuildPatchSingleSql(string schemaName, string entityName, string id, JsonPatch jsonPatchRequestBody);

  // TODO: Add OPTIONS command for security discovery
  // Not sure how we do this one yet. This will return the current user's access to the requested resource.
  // OptionsStatement BuildGetOptionsSql(Identity user, string schemaName, string entityName, string id);
}
