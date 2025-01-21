﻿using System.Data;
using System.Data.Common;
using DatabaseSchemaReader.DataSchema;
using Microsoft.VisualBasic;

namespace Patchwork.Schema;

public class SchemaDiscoveryBuilder
{
  public SchemaDiscoveryBuilder() { }

  public DatabaseMetadata ReadSchema(DbConnection connection)
  {
    var dbReader = new DatabaseSchemaReader.DatabaseReader(connection);

    var dbS = dbReader.AllSchemas();
    var dbT = dbReader.AllTables();
    var dbV = dbReader.AllViews();

    var tables = dbT.Select(t => new Table(t.Name, t.Description,
                                           t.Columns.Select(c => new Column(c.Name, c.Description, c.DbDataType,
                                                                            c.IsPrimaryKey, c.IsForeignKey, c.ForeignKeyTableName,
                                                                            c.IsAutoNumber,
                                                                            c.IsComputed,
                                                                            c.IsUniqueKey,
                                                                            c.IsIndexed)).ToList().AsReadOnly())
    );

    var views = dbV.Select(v => new View(v.Name, v.Description,
                                         v.Columns.Select(c => new Column(c.Name, c.Description, c.DbDataType,
                                                                          c.IsPrimaryKey, c.IsForeignKey, c.ForeignKeyTableName,
                                                                          c.IsAutoNumber,
                                                                          c.IsComputed,
                                                                          c.IsUniqueKey,
                                                                          c.IsIndexed)).ToList().AsReadOnly())
    );

    var metadata = new DatabaseMetadata(dbS.Select(s =>
    {
      var tables = dbT.Where(t => t.SchemaOwner == s.Name)
                      .Select(t => new Table(t.Name, t.Description,
                                             t.Columns.Select(c => new Column(c.Name, c.Description, c.DbDataType,
                                                                              c.IsPrimaryKey, c.IsForeignKey, c.ForeignKeyTableName,
                                                                              c.IsAutoNumber,
                                                                              c.IsComputed,
                                                                              c.IsUniqueKey,
                                                                              c.IsIndexed)).ToList().AsReadOnly())
                             ).ToList().AsReadOnly();

      var views = dbV.Where(t => t.SchemaOwner == s.Name)
                     .Select(v => new View(v.Name, v.Description,
                                           v.Columns.Select(c => new Column(c.Name, c.Description, c.DbDataType,
                                                                            c.IsPrimaryKey, c.IsForeignKey, c.ForeignKeyTableName,
                                                                            c.IsAutoNumber,
                                                                            c.IsComputed,
                                                                            c.IsUniqueKey,
                                                                            c.IsIndexed)).ToList().AsReadOnly())
                           ).ToList().AsReadOnly();

      return new Schema(s.Name, tables, views);

    }).ToList().AsReadOnly());


    return metadata;
  }
}
