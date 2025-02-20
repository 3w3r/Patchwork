using Patchwork.Authorization;
using Patchwork.Repository;
using Patchwork.SqlDialects;
using Patchwork.SqlDialects.PostgreSql;
using Patchwork.SqlDialects.Sqlite;
using Patchwork.SqlDialects.MsSql;
using Patchwork.SqlDialects.MySql;

namespace Patchwork.Api;

public static class Program
{
  public static void Main(string[] args)
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddSingleton<IPatchworkAuthorization, DefaultPatchworkAuthorization>();
    builder.Services.AddSingleton<ISqlDialectBuilder>(
#if SQLITE
      new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString())
#endif
#if POSTGRESQL
      new PostgreSqlDialectBuilder(ConnectionStringManager.GetPostgreSqlConnectionString())
#endif
#if MYSQL
      new MySqlDialectBuilder(ConnectionStringManager.GetMySqlConnectionString())
#endif
#if MSSQL
      new MsSqlDialectBuilder(ConnectionStringManager.GetMsSqlConnectionString())
#endif
      //new MsSqlDialectBuilder(ConnectionStringManager.GetMsSqlSurveysConnectionString())
      );
    builder.Services.AddScoped<IPatchworkRepository, PatchworkRepository>();

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddAuthentication();

    WebApplication app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
      app.UseSwagger();
      app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
  }
}
