using Azure;
using Patchwork.DbSchema;
using Patchwork.SqlStatements;

namespace Patchwork.SqlDialects;

public interface ISqlDialectBuilder
{
  DatabaseMetadata DiscoverSchema();

  SelectStatement BuildGetListSql(string schemaName, string entityName, string fields = "", string filter = "", string sort = "", int limit = 0, int offset = 0);
  SelectStatement BuildGetSingleSql(string schemaName, string entityName, string id, string fields = "", string include = "", DateTimeOffset? asOf = null);

  string BuildPatchListSql(string schemaName, string entityName, JsonPatchDocument jsonPatchRequestBody);
  UpdateStatement BuildPutSingleSql(string schemaName, string entityName, string id, string jsonResourceRequestBody);
  string BuildPatchSingleSql(string schemaName, string entityName, string id, JsonPatchDocument jsonPatchRequestBody);
  DeleteStatement BuildDeleteSingleSql(string schemaName, string entityName, string id);

}
