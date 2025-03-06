using System.Text.Json;
using Json.Patch;
using Patchwork.DbSchema;
using Patchwork.SqlStatements;

namespace Patchwork.SqlDialects;

public interface ISqlDialectBuilder
{
  string DefaultSchemaName { get; }

  WriterConnection GetWriterConnection();
  ReaderConnection GetReaderConnection();
  DatabaseMetadata DiscoverSchema();
  Entity FindEntity(string schemaName, string entityName);
  string GetPkValue(string schemaName, string entityName, object entityObject);
  bool HasPatchTrackingEnabled();

  SelectListStatement BuildGetListSql(string schemaName, string entityName, string fields = "", string filter = "", string sort = "", int limit = 0, int offset = 0);
  SelectResourceStatement BuildGetSingleSql(string schemaName, string entityName, string id, string fields = "", string include = "", DateTimeOffset? asOf = null);
  SelectEventLogStatement BuildGetEventLogSql(string schemaName, string entityName, string id, DateTimeOffset asOf);

  InsertStatement BuildPostSingleSql(string schemaName, string entityName, JsonDocument jsonResourceRequestBody);
  UpdateStatement BuildPutSingleSql(string schemaName, string entityName, string id, JsonDocument jsonResourceRequestBody);
  DeleteStatement BuildDeleteSingleSql(string schemaName, string entityName, string id);

  PatchStatement BuildPatchListSql(string schemaName, string entityName, JsonPatch jsonPatchRequestBody);
  PatchStatement BuildPatchSingleSql(string schemaName, string entityName, string id, JsonPatch jsonPatchRequestBody);

  JsonPatch BuildDiffAsJsonPatch(string original, string modified);
  JsonPatch BuildDiffAsJsonPatch(JsonDocument original, JsonDocument modified);

  InsertStatement GetInsertStatementForPatchworkLog(string schemaName, string entityName, string id, JsonPatch jsonPatchRequestBody);

  // TODO: Add OPTIONS command for security discovery
  // Not sure how we do this one yet. This will return the current user's access to the requested resource.
  // OptionsStatement BuildGetOptionsSql(Identity user, string schemaName, string entityName, string id);
}
