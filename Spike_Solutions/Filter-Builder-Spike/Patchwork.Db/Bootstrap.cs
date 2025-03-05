using FluentMigrator;

namespace Patchwork.Db;

[Migration(202501120930)]
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
