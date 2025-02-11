
using Microsoft.Extensions.DependencyInjection.Extensions;
using Patchwork.Authorization;
using Patchwork.SqlDialects;
using Patchwork.SqlDialects.PostgreSql;

namespace Patchwork.Api
{
  public static class Program
  {
    public static void Main(string[] args)
    {
      var builder = WebApplication.CreateBuilder(args);

      // Add services to the container.
      builder.Services.AddSingleton<IPatchworkAuthorization, DefaultPatchworkAuthorization>();
      builder.Services.AddSingleton<ISqlDialectBuilder>(
        new PostgreSqlDialectBuilder("Host=npgsql;Port=5432;Database=classicmodels;User ID=admin;Password=LocalNpgSql;Pooling=true;SearchPath=public;")
        );

      builder.Services.AddControllers();
      // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
      builder.Services.AddEndpointsApiExplorer();
      builder.Services.AddSwaggerGen();

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
}
