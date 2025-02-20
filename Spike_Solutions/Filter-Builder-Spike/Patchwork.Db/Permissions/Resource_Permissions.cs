using FluentMigrator;

namespace Patchwork.Db.Permissions;

[Migration(202502200811)]
public class Resource_Permissions : AutoReversingMigration
{
  public override void Up()
  {
    IfDatabase(t => t != ProcessorId.SQLite).Create
      .Table("patchwork_resource_permission")
      .InSchema("patchwork")
      .WithColumn("pk").AsInt64().Unique().Identity().PrimaryKey()
      .WithColumn("role").AsString(64).Nullable().Indexed("ix_resource_permission_role")
      .WithColumn("user").AsString(64).Nullable().Indexed("ix_resource_permission_user")
      .WithColumn("domain").AsString(64).NotNullable().Indexed("ix_resource_permission_domain")
      .WithColumn("entity").AsString(64).NotNullable().Indexed("ix_resource_permission_entity")
      .WithColumn("id").AsString(64).NotNullable().Indexed().Indexed("ix_resource_permission_id")
      .WithColumn("access").AsString().NotNullable().WithDefaultValue("None")
      .WithColumn("assigned_date").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

    IfDatabase(t => t == ProcessorId.SQLite).Create
      .Table("patchwork_resource_permission")
      .WithColumn("pk").AsInt64().Unique().Identity().PrimaryKey()
      .WithColumn("role").AsString(64).Nullable().Indexed("ix_resource_permission_role")
      .WithColumn("user").AsString(64).Nullable().Indexed("ix_resource_permission_user")
      .WithColumn("domain").AsString(64).NotNullable().Indexed("ix_resource_permission_domain")
      .WithColumn("entity").AsString(64).NotNullable().Indexed("ix_resource_permission_entity")
      .WithColumn("id").AsString(64).NotNullable().Indexed().Indexed("ix_resource_permission_id")
      .WithColumn("access").AsString().NotNullable().WithDefaultValue("None")
      .WithColumn("assigned_date").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);
  }
}
