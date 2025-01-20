using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using DatabaseSchemaReader.DataSchema;

namespace Patchwork.Schema;

public class SchemaDiscoveryBuilder
{
  private string _connectionString;
  public SchemaDiscoveryBuilder(string connectionString)
  {
    _connectionString = connectionString;
  }
  public DatabaseMetadata ReadSchema(DbConnection connection)
  {
    var dbReader = new DatabaseSchemaReader.DatabaseReader(connection);

    var metadata = new DatabaseMetadata(
      dbReader.AllSchemas().AsReadOnly(),
      dbReader.AllTables().AsReadOnly(),
      dbReader.AllViews().AsReadOnly()
    );

    return metadata;
  }
}

public record DatabaseMetadata(
  ReadOnlyCollection<DatabaseDbSchema> Schemas,
  ReadOnlyCollection<DatabaseTable> Tables,
  ReadOnlyCollection<DatabaseView> Views);
