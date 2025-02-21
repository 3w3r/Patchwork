using FluentMigrator;

namespace Patchwork.Db.EventLog;

[Migration(202502120945)]
public class EventLog_Table : AutoReversingMigration
{
  public override void Up()
  {
    IfDatabase(t => t != ProcessorId.SQLite).Create
      .Table("patchwork_event_log")
      .InSchema("patchwork")
      .WithColumn("pk").AsInt64().Unique().Identity().PrimaryKey()
      .WithColumn("event_date").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
      .WithColumn("domain").AsString(64).NotNullable()
      .WithColumn("entity").AsString(64).NotNullable()
      .WithColumn("id").AsString(64).NotNullable()
      .WithColumn("patch").AsString().NotNullable().WithDefaultValue("{}");

    IfDatabase(t => t == ProcessorId.SQLite).Create
      .Table("patchwork_event_log")
      .WithColumn("pk").AsInt64().Unique().Identity().PrimaryKey()
      .WithColumn("event_date").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
      .WithColumn("domain").AsString(64).NotNullable()
      .WithColumn("entity").AsString(64).NotNullable()
      .WithColumn("id").AsString(64).NotNullable()
      .WithColumn("patch").AsString().NotNullable().WithDefaultValue("{}");
  }
}

[Migration(202502120955)]
public class EventLog_Table_Indexes : AutoReversingMigration
{
  public override void Up()
  {
    IfDatabase(t => t != ProcessorId.SQLite).Create
      .Index("ix_event_log_domain").OnTable("patchwork_event_log").InSchema("patchwork").OnColumn("domain");
    IfDatabase(t => t != ProcessorId.SQLite).Create
      .Index("ix_event_log_entity").OnTable("patchwork_event_log").InSchema("patchwork").OnColumn("entity");
    IfDatabase(t => t != ProcessorId.SQLite).Create
      .Index("ix_event_log_id").OnTable("patchwork_event_log").InSchema("patchwork").OnColumn("id");

    IfDatabase(t => t == ProcessorId.SQLite).Create
      .Index("ix_event_log_domain").OnTable("patchwork_event_log").OnColumn("domain");
    IfDatabase(t => t == ProcessorId.SQLite).Create
      .Index("ix_event_log_entity").OnTable("patchwork_event_log").OnColumn("entity");
    IfDatabase(t => t == ProcessorId.SQLite).Create
      .Index("ix_event_log_id").OnTable("patchwork_event_log").OnColumn("id");
  }
}