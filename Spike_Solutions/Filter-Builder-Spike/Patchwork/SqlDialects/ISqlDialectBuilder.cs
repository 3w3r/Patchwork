using Patchwork.DbSchema;

namespace Patchwork.SqlDialects;


public interface ISqlDialectBuilder
{
  DatabaseMetadata DiscoverSchema();
  string BuildSelectClause(string entityName);
  string BuildJoinClause(string includeString, string entityName);
  string BuildWhereClause(string filterString);
  string BuildOrderByClause(string sort, string pkName);
  string BuildLimitOffsetClause(int limit, int offset);

}
