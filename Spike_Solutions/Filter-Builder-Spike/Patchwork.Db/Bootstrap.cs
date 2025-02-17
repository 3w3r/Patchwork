using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentMigrator;

namespace Patchwork.Db;

[Migration(202502120931)]
public class Bootstrap : Migration
{

  public override void Up()
  {
    IfDatabase(t => t != ProcessorId.SQLite)
              .Create.Schema("patchwork");
  }

  public override void Down()
  {
    IfDatabase(t => t != ProcessorId.SQLite)
              .Delete.Schema("patchwork");
  }
}
