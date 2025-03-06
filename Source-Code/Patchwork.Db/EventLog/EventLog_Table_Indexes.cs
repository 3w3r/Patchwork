using FluentMigrator;

namespace Patchwork.Db.EventLog;

[Migration(202501120955)]
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