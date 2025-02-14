using System;
using System.Linq;
using System.Runtime.CompilerServices;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;

using Microsoft.Extensions.DependencyInjection;

namespace Patchwork.Db;

static class Program
{
  static void Main(string[] args)
  {
    using (var serviceProvider = CreateServices())
    using (var scope = serviceProvider.CreateScope())
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
        .ConfigureRunner(rb => rb
#if SQLITE
            // Add SQLite support to FluentMigrator
            .AddSQLite().WithGlobalConnectionString(ConnectionStringManager.GetSqliteConnectionString())
#endif
#if MSSQL
            .AddSqlServer2016().WithGlobalConnectionString(ConnectionStringManager.GetMsSqlConnectionString())
#endif
#if POSTGRESQL
            .AddPostgres().WithGlobalConnectionString(ConnectionStringManager.GetPostgreSqlConnectionString())
#endif
#if MYSQL
            .AddMySql8().WithGlobalConnectionString(ConnectionStringManager.GetMySqlConnectionString())
#endif
            // Define the assembly containing the migrations
            .ScanIn(typeof(Bootstrap).Assembly).For.Migrations())
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
    var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

    // Execute the migrations
    runner.MigrateUp();
  }
}