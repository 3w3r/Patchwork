using System.Data;
using System.Data.Common;
using DatabaseSchemaReader.DataSchema;

namespace Patchwork.DbSchema;

public class SchemaDiscoveryBuilder
{
  public SchemaDiscoveryBuilder() { }

  public DatabaseMetadata ReadSchema(DbConnection connection)
  {
    DatabaseSchemaReader.DatabaseReader dbReader = new DatabaseSchemaReader.DatabaseReader(connection);

    IList<DatabaseDbSchema> dbS = dbReader.AllSchemas();
    if (dbS.Count == 0)
      dbS.Add(new DatabaseDbSchema() { Name = "dbo" });
    IList<DatabaseTable> dbT = dbReader.AllTables();

    IList<DatabaseView> dbV = dbReader.AllViews();

    DatabaseMetadata metadata = new DatabaseMetadata(dbS.Select(s =>
    {
      List<Entity> tables = dbT.Where(t => t.SchemaOwner == s.Name || (t.SchemaOwner == "" && s.Name == "dbo"))
                      .Select(t => new Entity(t.Name, t.Description, t.SchemaOwner,
                                              t.Columns.Select(c => new Column(c.Name,
                                                                               c.Description,
                                                                               c.DbDataType,
                                                                               c.IsPrimaryKey,
                                                                               c.IsForeignKey,
                                                                               c.ForeignKeyTableName,
                                                                               c.IsAutoNumber,
                                                                               c.IsComputed,
                                                                               c.IsUniqueKey,
                                                                               c.IsIndexed)).ToList())
                             ).ToList();

      List<Entity> views = dbV.Where(t => t.SchemaOwner == s.Name || (t.SchemaOwner == "" && s.Name == "dbo"))
                     .Select(v => new Entity(v.Name, v.Description, v.SchemaOwner,
                                             v.Columns.Select(c => new Column(c.Name,
                                                                              c.Description,
                                                                              c.DbDataType,
                                                                              c.IsPrimaryKey,
                                                                              c.IsForeignKey,
                                                                              c.ForeignKeyTableName,
                                                                              c.IsAutoNumber,
                                                                              c.IsComputed,
                                                                              c.IsUniqueKey,
                                                                              c.IsIndexed)).ToList())
                           ).ToList();

      return new Schema(s.Name, tables, views);

    }).ToList());

    return metadata;
  }
}
