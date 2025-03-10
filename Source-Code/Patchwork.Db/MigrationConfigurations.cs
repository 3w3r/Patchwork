namespace Patchwork.Db;

public static class MigrationConfigurations
{
  public static DbTypeEnum DbType { get; set; } = DbTypeEnum.Sqlite;
}
public enum DbTypeEnum
{
  Unknown = 0,
  Sqlite = 1,
  MsSql = 2,
  MySql = 3,
  PostgreSql = 4
}