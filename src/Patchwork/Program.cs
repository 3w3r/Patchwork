using Microsoft.AspNetCore.Mvc;
using Patchwork.SqlDialects;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<ISqlDialectBuilder>(x => {
    try
    {
        switch (
    builder.Configuration["DataProvider"])
        {
            case "MySql":
                return new MySqlDialectBuilder(builder.Configuration["ConnectionString"]);
            case "MsSql":
                return new MsSqlDialectBuilder(builder.Configuration["ConnectionString"]);
            case "PostgreSql":
                return new PostgreSqlDialectBuilder(builder.Configuration["ConnectionString"]);
            default:
                throw new NotSupportedException();
        }
    }
    catch (Exception ex) {
        throw new Exception("Invalid appsettings.json. Check your connection string and ensure your data provider is valid.");
    }
});
builder.Services.AddControllersWithViews();
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
