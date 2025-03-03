
using System.Reflection;
using System.Text.Json;

namespace Patchwork;

public static class ConnectionStringManager
{
  private static JsonDocument? Configfile { get; set; } = null;

  private static void LoadConfigFile()
  {
    if (Configfile == null)
    {
      string location = Assembly.GetExecutingAssembly().Location;
      location = location.Contains('/')
        ? location.Substring(0, location.LastIndexOf("/"))
        : location.Substring(0, location.LastIndexOf("\\"));
      string configFile = Path.Combine(location, "appsettings.local.json");
      string configJson = File.ReadAllText(configFile);
      Configfile = JsonDocument.Parse(configJson);
    }
    if (Configfile == null)
      throw new FileNotFoundException("Could not load configuration file.");
  }

  public static string GetMySqlConnectionString()
  {
    LoadConfigFile();
    string? connectionString = Configfile!.RootElement.GetProperty("ConnectionStrings").GetProperty("MySql").GetString();
    if (string.IsNullOrEmpty(connectionString))
      throw new KeyNotFoundException("MySql not found in configuration file.");
    return connectionString;
  }
  public static string GetMsSqlConnectionString()
  {
    LoadConfigFile();
    string? connectionString = Configfile!.RootElement.GetProperty("ConnectionStrings").GetProperty("MsSql").GetString();
    if (string.IsNullOrEmpty(connectionString))
      throw new KeyNotFoundException("MsSql not found in configuration file.");
    return connectionString;
  }
  public static string GetMsSqlSurveysConnectionString()
  {
    LoadConfigFile();
    string? connectionString = Configfile!.RootElement.GetProperty("ConnectionStrings").GetProperty("MsSql_Surveys").GetString();
    if (string.IsNullOrEmpty(connectionString))
      throw new KeyNotFoundException("MsSql not found in configuration file.");
    return connectionString;
  }
  public static string GetPostgreSqlConnectionString()
  {
    LoadConfigFile();
    string? connectionString = Configfile!.RootElement.GetProperty("ConnectionStrings").GetProperty("PostgreSql").GetString();
    if (string.IsNullOrEmpty(connectionString))
      throw new KeyNotFoundException("PostgreSql not found in configuration file.");
    return connectionString;
  }
  public static string GetSqliteConnectionString()
  {
    LoadConfigFile();
    string? connectionString = Configfile!.RootElement.GetProperty("ConnectionStrings").GetProperty("Sqlite").GetString();
    if (string.IsNullOrEmpty(connectionString))
      throw new KeyNotFoundException("Sqlite not found in configuration file.");
    return connectionString;
  }

}
