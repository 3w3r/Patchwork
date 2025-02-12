using Patchwork.Authorization;
using Patchwork.SqlDialects;
using Patchwork.SqlDialects.MsSql;
using Patchwork.SqlDialects.PostgreSql;

namespace Patchwork.Api;

public static class Program
{
  public static void Main(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddSingleton<IPatchworkAuthorization, DefaultPatchworkAuthorization>();
    builder.Services.AddSingleton<ISqlDialectBuilder>(
      //new MySqlDialectBuilder(ConnectionStringManager.GetMySqlConnectionString())
      new PostgreSqlDialectBuilder(ConnectionStringManager.GetPostgreSqlConnectionString())
      //new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString())
      //new MsSqlDialectBuilder(ConnectionStringManager.GetMsSqlConnectionString())
      //new MsSqlDialectBuilder(ConnectionStringManager.GetMsSqlSurveysConnectionString())
      );

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddAuthentication();

    var app = builder.Build();

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
