using Azure;
using System.Collections.Specialized;
using Patchwork.DbSchema;
using System.Reflection.Metadata;

namespace Patchwork.SqlDialects;

public interface ISqlDialectBuilder
{
  DatabaseMetadata DiscoverSchema();

  string BuildGetListSql(string schemaName, string entityName
    , string fields = ""
    , string filter = ""
    , string sort = ""
    , int limit = 0
    , int offset = 0
    );
  string BuildPatchListSql(string schemaName, string entityName, JsonPatchDocument jsonPatchRequestBody);
  string BuildGetSingleSql(string schemaName, string entityName, string id
    , string fields = ""
    , string include = ""
    , DateTimeOffset? asOf = null);
  string BuildPutSingleSql(string schemaName, string entityName, string id, string jsonResourceRequestBody);
  string BuildPatchSingleSql(string schemaName, string entityName, string id, JsonPatchDocument jsonPatchRequestBody);
  string BuildDeleteSingleSql(string schemaName, string entityName, string id);

  string BuildSelectClause(string fields, string entityName);
  string BuildJoinClause(string includeString, string entityName);
  string BuildWhereClause(string filterString, string entityName);
  string BuildOrderByClause(string sort, string entityName);
  string BuildLimitOffsetClause(int limit, int offset);
}
