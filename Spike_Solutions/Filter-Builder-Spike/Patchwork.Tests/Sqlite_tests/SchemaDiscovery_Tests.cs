using Microsoft.Data.Sqlite;
using Patchwork.DbSchema;

namespace Patchwork.Tests.Sqlite_tests;

public class SchemaDiscovery_Tests
{
  [Fact]
  public void SchemaDiscovery_CanFindTables()
  {
    // Arrange
    using SqliteConnection connection = new SqliteConnection(ConnectionStringManager.GetSqliteConnectionString());
    connection.Open();

    // Act
    SchemaDiscoveryBuilder builder = new SchemaDiscoveryBuilder();
    DatabaseMetadata metadata = builder.ReadSchema(connection);

    // Assert
    Assert.NotNull(metadata);
    Assert.NotEmpty(metadata.Schemas);
    Assert.NotEmpty(metadata.Schemas.First().Tables);
    Assert.NotEmpty(metadata.Schemas.First().Views);

    IList<Entity> tables = metadata.Schemas.First().Tables;
    Assert.Contains("customers", tables.Select(t => t.Name));
    Assert.Contains("employees", tables.Select(t => t.Name));
    Assert.Contains("offices", tables.Select(t => t.Name));
    Assert.Contains("orderdetails", tables.Select(t => t.Name));
    Assert.Contains("orders", tables.Select(t => t.Name));
    Assert.Contains("payments", tables.Select(t => t.Name));
    Assert.Contains("productlines", tables.Select(t => t.Name));
    Assert.Contains("products", tables.Select(t => t.Name));

    IList<Entity> views = metadata.Schemas.First().Views;
    Assert.Contains("CustomerSpendingByProductLine", views.Select(t => t.Name));

    connection.Close();
  }
}
