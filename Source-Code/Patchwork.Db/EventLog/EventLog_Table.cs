using FluentMigrator;

namespace Patchwork.Db.EventLog;

[Migration(202501120945)]
public class EventLog_Table : AutoReversingMigration
{
  public override void Up()
  {
    IfDatabase(t => t != ProcessorId.SQLite).Create
      .Table("patchwork_event_log")
      .InSchema("patchwork")
      .WithColumn("pk").AsInt64().Unique().Identity().PrimaryKey()
      .WithColumn("event_date").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
      .WithColumn("http_method").AsInt16().NotNullable().WithDefaultValue(0)
      .WithColumn("domain").AsString(64).NotNullable()
      .WithColumn("entity").AsString(64).NotNullable()
      .WithColumn("id").AsString(64).NotNullable()
      .WithColumn("status").AsString(64).NotNullable().WithDefaultValue(0)
      .WithColumn("patch").AsString().NotNullable().WithDefaultValue("{}");

    IfDatabase(t => t == ProcessorId.SQLite).Create
      .Table("patchwork_event_log")
      .WithColumn("pk").AsInt64().Unique().Identity().PrimaryKey()
      .WithColumn("event_date").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
      .WithColumn("http_method").AsInt16().NotNullable().WithDefaultValue(0)
      .WithColumn("domain").AsString(64).NotNullable()
      .WithColumn("entity").AsString(64).NotNullable()
      .WithColumn("id").AsString(64).NotNullable()
      .WithColumn("status").AsString(64).NotNullable().WithDefaultValue(0)
      .WithColumn("patch").AsString().NotNullable().WithDefaultValue("{}");
  }
}
