using FluentMigrator;

namespace Patchwork.Db.Permissions;

[Migration(202502200809)]
public class List_Permissions : AutoReversingMigration
{
  public override void Up()
  {
    IfDatabase(t => t != ProcessorId.SQLite).Create
      .Table("patchwork_entity_permission")
      .InSchema("patchwork")
      .WithColumn("pk").AsInt64().Unique().Identity().PrimaryKey()
      .WithColumn("role").AsString(64).Nullable().Indexed("ix_entity_permission_role")
      .WithColumn("user").AsString(64).Nullable().Indexed("ix_entity_permission_user")
      .WithColumn("domain").AsString(64).NotNullable().Indexed("ix_entity_permission_domain")
      .WithColumn("entity").AsString(64).NotNullable().Indexed("ix_entity_permission_entity")
      .WithColumn("access").AsString().NotNullable().WithDefaultValue("None")
      .WithColumn("assigned_date").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

    IfDatabase(t => t == ProcessorId.SQLite).Create
      .Table("patchwork_entity_permission")
      .WithColumn("pk").AsInt64().Unique().Identity().PrimaryKey()
      .WithColumn("role").AsString(64).Nullable().Indexed("ix_entity_permission_role")
      .WithColumn("user").AsString(64).Nullable().Indexed("ix_entity_permission_user")
      .WithColumn("domain").AsString(64).NotNullable().Indexed("ix_entity_permission_domain")
      .WithColumn("entity").AsString(64).NotNullable().Indexed("ix_entity_permission_entity")
      .WithColumn("access").AsString().NotNullable().WithDefaultValue("None")
      .WithColumn("assigned_date").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);
  }
}
