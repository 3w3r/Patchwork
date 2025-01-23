using Patchwork.DbSchema;

namespace Patchwork.SqlDialects;

public interface ISqlDialectBuilder
{
  DatabaseMetadata DiscoverSchema();
  string BuildSelectClause(string fields, string entityName);
  string BuildJoinClause(string includeString, string entityName);
  string BuildWhereClause(string filterString, string entityName);
  string BuildOrderByClause(string sort, string pkName, string entityName);
  string BuildLimitOffsetClause(int limit, int offset);
}
