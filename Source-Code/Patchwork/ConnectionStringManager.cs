
using System.Reflection;
using System.Text.Json;

namespace Patchwork;

public static class ConnectionStringManager
{
  private static JsonDocument? Configfile { get; set; } = null;

  private static void LoadConfigFile()
  {
    // Check if the configuration file has already been loaded
    if (Configfile == null)
    {
      // Get the location of the executing assembly
      var location = Assembly.GetExecutingAssembly().Location;

      // Determine the correct path separator based on the operating system
      location = location.Contains('/')
        ? location.Substring(0, location.LastIndexOf("/"))
        : location.Substring(0, location.LastIndexOf("\\"));

      // Construct the path to the local configuration file
      var configFile = Path.Combine(location, "connectionstrings.local.json");

      // Read the contents of the configuration file
      var configJson = File.ReadAllText(configFile);

      // Parse the JSON configuration into a JsonDocument
      Configfile = JsonDocument.Parse(configJson);
    }

    // If the configuration file was not loaded, throw an exception
    if (Configfile == null)
      throw new FileNotFoundException("Could not load configuration file.");
  }

  public static string GetMySqlConnectionString()
  {
    LoadConfigFile();
    var connectionString = Configfile!.RootElement.GetProperty("ConnectionStrings").GetProperty("MySql").GetString();
    if (string.IsNullOrEmpty(connectionString))
      throw new KeyNotFoundException("MySql not found in configuration file.");
    return connectionString;
  }
  public static string GetMsSqlConnectionString()
  {
    LoadConfigFile();
    var connectionString = Configfile!.RootElement.GetProperty("ConnectionStrings").GetProperty("MsSql").GetString();
    if (string.IsNullOrEmpty(connectionString))
      throw new KeyNotFoundException("MsSql not found in configuration file.");
    return connectionString;
  }

  public static string GetPostgreSqlConnectionString()
  {
    LoadConfigFile();
    var connectionString = Configfile!.RootElement.GetProperty("ConnectionStrings").GetProperty("PostgreSql").GetString();
    if (string.IsNullOrEmpty(connectionString))
      throw new KeyNotFoundException("PostgreSql not found in configuration file.");
    return connectionString;
  }
  public static string GetSqliteConnectionString()
  {
    try
    {
      LoadConfigFile();
      var connectionString = Configfile!.RootElement.GetProperty("ConnectionStrings").GetProperty("Sqlite").GetString();
      if (string.IsNullOrEmpty(connectionString))
        throw new KeyNotFoundException("Sqlite not found in configuration file.");
      return connectionString;
    } catch
    {
      return "Data Source=./Test-Data/Products.db";
    }
  }
}
