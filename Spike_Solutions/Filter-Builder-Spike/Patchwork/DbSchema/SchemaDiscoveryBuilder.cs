using System.Data;
using DatabaseSchemaReader.DataSchema;
using Patchwork.SqlDialects;

namespace Patchwork.DbSchema;

public class SchemaDiscoveryBuilder
{
  public DatabaseMetadata ReadSchema(ActiveConnection connection)
  {
    DatabaseSchemaReader.DatabaseReader dbReader = new DatabaseSchemaReader.DatabaseReader(connection.Transaction);

    IList<DatabaseDbSchema> dbS = dbReader.AllSchemas();
    if (dbS.Count == 0)
      dbS.Add(new DatabaseDbSchema() { Name = "dbo" });

    IList<DatabaseTable> dbT = dbReader.AllTables().ToList();
    dbT = dbT.Where(t => !t.SchemaOwner.Equals("information_schema", StringComparison.OrdinalIgnoreCase)).ToList();
    dbT = dbT.Where(t => !(connection.GetType().Name.Contains("npgsql", StringComparison.OrdinalIgnoreCase) && t.Name.StartsWith("pg_"))).ToList();

    IList<DatabaseView> dbV = dbReader.AllViews().ToList();
    dbV = dbV.Where(v => !v.SchemaOwner.Equals("information_schema", StringComparison.OrdinalIgnoreCase)).ToList();
    dbV = dbV.Where(v => !(connection.GetType().Name.Contains("npgsql", StringComparison.OrdinalIgnoreCase) && v.Name.StartsWith("pg_"))).ToList();

    var schemas = dbS.Select(s =>
    {
      List<Entity> tables = dbT.Where(t => t.SchemaOwner == s.Name || (t.SchemaOwner == "" && s.Name == "dbo"))
                      .Select(t => new Entity(t.Name, t.Description, t.SchemaOwner, false,
                                              t.Columns.Select(c => new Column(c.Name,
                                                                               c.Description,
                                                                               c.DataType == null ? typeof(string) : c.NetDataType(),
                                                                               c.IsPrimaryKey,
                                                                               c.IsForeignKey,
                                                                               c.ForeignKeyTableName,
                                                                               c.IsAutoNumber,
                                                                               c.IsComputed,
                                                                               c.IsUniqueKey,
                                                                               c.IsIndexed)).ToList())
                             ).ToList();

      List<Entity> views = dbV.Where(t => t.SchemaOwner == s.Name || (t.SchemaOwner == "" && s.Name == "dbo"))
                     .Select(v => new Entity(v.Name, v.Description, v.SchemaOwner, true,
                                             v.Columns.Select(c => new Column(c.Name,
                                                                              c.Description,
                                                                              c.DataType == null ? typeof(string) : c.NetDataType(),
                                                                              c.IsPrimaryKey,
                                                                              c.IsForeignKey,
                                                                              c.ForeignKeyTableName,
                                                                              c.IsAutoNumber,
                                                                              true, // All VIEW columns are computed and read only
                                                                              c.IsUniqueKey,
                                                                              c.IsIndexed)).ToList())
                           ).ToList();

      return new Schema(s.Name, tables, views);
    }).ToList();

    // ensure we do not return any empty schemas
    var populatedSchemas = schemas.Where(s => s.Tables.Any() || s.Views.Any()).ToList();
    var hasPatchTracking = schemas.Any(s => s.Tables.Any(t => string.Equals(t.Name, "patchwork_event_log", StringComparison.OrdinalIgnoreCase)));
    return new DatabaseMetadata(populatedSchemas, hasPatchTracking);
  }
}
