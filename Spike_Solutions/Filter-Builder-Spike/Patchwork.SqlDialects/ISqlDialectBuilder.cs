namespace Patchwork.SqlDialects;


public interface ISqlDialectBuilder
{
  string BuildSelectClause(string tableName, string schemaName);
  string BuildWhereClause(string filterString);
  string BuildOrderByClause(string sort, string pkName);

}
