using FluentMigrator.Runner;

using Microsoft.Extensions.DependencyInjection;

namespace Patchwork.Db;

static class Program
{
  static void Main(string[] args)
  {
    using (ServiceProvider serviceProvider = CreateServices())
    using (IServiceScope scope = serviceProvider.CreateScope())
    {
      // Put the database update into a scope to ensure
      // that all resources will be disposed.
      Console.WriteLine("---------------------[  PERFORMING DATABASE MIGRATION  ]-----------------------");
      UpdateDatabase(scope.ServiceProvider);
      Console.WriteLine("---------------------[  DATABASE MIGRATION COMPLETED   ]-----------------------");
    }
    Environment.Exit(0);
  }

  /// <summary>
  /// Configure the dependency injection services
  /// </summary>
  private static ServiceProvider CreateServices()
  {
    return new ServiceCollection()
        // Add common FluentMigrator services
        .AddFluentMigratorCore()
        .ConfigureRunner(rb => {
          var success = Enum.TryParse(Environment.GetEnvironmentVariable("DBTYPE"), out DbTypeEnum dbType);
          if (success)
            MigrationConfigurations.DbType = dbType;

          if (MigrationConfigurations.DbType == DbTypeEnum.MsSql) rb.AddSqlServer2016().WithGlobalConnectionString(ConnectionStringManager.GetMsSqlConnectionString());
          else if (MigrationConfigurations.DbType == DbTypeEnum.PostgreSql) rb.AddPostgres().WithGlobalConnectionString(ConnectionStringManager.GetPostgreSqlConnectionString());
          else if (MigrationConfigurations.DbType == DbTypeEnum.MySql) rb.AddMySql8().WithGlobalConnectionString(ConnectionStringManager.GetMySqlConnectionString());
          else rb.AddSQLite().WithGlobalConnectionString(ConnectionStringManager.GetSqliteConnectionString());
          
          // Define the assembly containing the migrations
          rb.ScanIn(typeof(Bootstrap).Assembly).For.Migrations();
        })
        // Enable logging to console in the FluentMigrator way
        .AddLogging(lb => lb.AddFluentMigratorConsole())
        // Build the service provider
        .BuildServiceProvider(false);
  }

  /// <summary>
  /// Update the database
  /// </summary>
  private static void UpdateDatabase(IServiceProvider serviceProvider)
  {
    // Instantiate the runner
    IMigrationRunner runner = serviceProvider.GetRequiredService<IMigrationRunner>();

    // Execute the migrations
    runner.MigrateUp();
  }
}
