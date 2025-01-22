using System.Data;
using System.Data.Common;
using DatabaseSchemaReader.DataSchema;
using Microsoft.VisualBasic;

namespace Patchwork.DbSchema;

public class SchemaDiscoveryBuilder
{
  public SchemaDiscoveryBuilder() { }

  public DatabaseMetadata ReadSchema(DbConnection connection)
  {
    var dbReader = new DatabaseSchemaReader.DatabaseReader(connection);

    var dbS = dbReader.AllSchemas();
    var dbT = dbReader.AllTables();
    var dbV = dbReader.AllViews();

    var metadata = new DatabaseMetadata(dbS.Select(s =>
    {
      var tables = dbT.Where(t => t.SchemaOwner == s.Name)
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

      var views = dbV.Where(t => t.SchemaOwner == s.Name)
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
