using System.Data;
using DatabaseSchemaReader.DataSchema;
using Patchwork.SqlDialects;

namespace Patchwork.DbSchema;

public class SchemaDiscoveryBuilder
{
  /// <summary>
/// Reads the database schema using the provided connection.
/// </summary>
/// <param name="connection">The connection to the database.</param>
/// <returns>A DatabaseMetadata object containing the discovered schema.</returns>
public DatabaseMetadata ReadSchema(WriterConnection connection)
{
  // Initialize the DatabaseReader with the provided connection
  DatabaseSchemaReader.DatabaseReader dbReader = new DatabaseSchemaReader.DatabaseReader(connection.Transaction);

  // Retrieve all schemas from the database
  IList<DatabaseDbSchema> dbS = dbReader.AllSchemas();

  // If no schemas are found, add a default schema named "dbo"
  if (dbS.Count == 0)
    dbS.Add(new DatabaseDbSchema() { Name = "dbo" });

  // Retrieve all tables from the database
  IList<DatabaseTable> dbT = dbReader.AllTables().ToList();

  // Filter out system schemas
  dbT = dbT.Where(t => !t.SchemaOwner.Equals("information_schema", StringComparison.OrdinalIgnoreCase)).ToList();
  dbT = dbT.Where(t => !t.SchemaOwner.Equals("pg_catalog", StringComparison.OrdinalIgnoreCase)).ToList();

  // Filter out PostgreSQL system tables
  dbT = dbT.Where(t => !(connection.GetType().Name.Contains("npgsql", StringComparison.OrdinalIgnoreCase) && t.Name.StartsWith("pg_"))).ToList();

  // Retrieve all views from the database
  IList<DatabaseView> dbV = dbReader.AllViews().ToList();

  // Filter out system schemas
  dbV = dbV.Where(v => !v.SchemaOwner.Equals("information_schema", StringComparison.OrdinalIgnoreCase)).ToList();
  dbV = dbV.Where(t => !t.SchemaOwner.Equals("pg_catalog", StringComparison.OrdinalIgnoreCase)).ToList();

  // Filter out PostgreSQL system views
  dbV = dbV.Where(v => !(connection.GetType().Name.Contains("npgsql", StringComparison.OrdinalIgnoreCase) && v.Name.StartsWith("pg_"))).ToList();

  // Map the database schema to the Schema class
  List<Schema> schemas = dbS.Select(s =>
  {
    // Map tables to the Entity class
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

    // Map views to the Entity class
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

    // Return a new Schema object
    return new Schema(s.Name, tables, views);
  }).ToList();

  // Filter out any empty schemas
  List<Schema> populatedSchemas = schemas.Where(s => s.Tables.Any() || s.Views.Any()).ToList();

  // Check if the "patchwork_event_log" table exists in any schema
  bool hasPatchTracking = schemas.Any(s => s.Tables.Any(t => string.Equals(t.Name, "patchwork_event_log", StringComparison.OrdinalIgnoreCase)));

  // Return a new DatabaseMetadata object
  return new DatabaseMetadata(populatedSchemas, hasPatchTracking);
}
}
