using Patchwork.DbSchema;
using System.Data.Common;

namespace Patchwork.SqlDialects
{
  public abstract class SqlDialectBuilderBase : ISqlDialectBuilder
  {

    protected readonly string _connectionString;
    protected DatabaseMetadata? _metadata = null;

    public SqlDialectBuilderBase(string connectionString)
    {
      _connectionString = connectionString;
    }
    public SqlDialectBuilderBase(DatabaseMetadata metadata)
    {
      _connectionString = string.Empty;
      _metadata = metadata;
    }

    protected abstract DbConnection GetConnection();
    public virtual DatabaseMetadata DiscoverSchema()
    {
      if (_metadata != null) return _metadata;

      var schemaDiscoveryBuilder = new SchemaDiscoveryBuilder();
      using (var connection = GetConnection())
      {
        return schemaDiscoveryBuilder.ReadSchema(connection);
      }
    }

    public abstract string BuildSelectClause(string entityName);
    public abstract string BuildJoinClause(string includeString, string entityName);
    public abstract string BuildWhereClause(string filterString);
    public abstract string BuildOrderByClause(string sort, string pkName);
    public abstract string BuildLimitOffsetClause(int limit, int offset);

  }
}
