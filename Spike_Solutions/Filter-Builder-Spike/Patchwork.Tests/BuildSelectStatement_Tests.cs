using Microsoft.Data.Sqlite;
using Patchwork.DbSchema;
using Patchwork.SqlDialects;

namespace Patchwork.Tests;

public class SchemaDiscovery_Tests
{
  [Fact]
  public void SchemaDiscovery_CanFindTables()
  {
    // Arrange
    using SqliteConnection connection = new SqliteConnection("Data Source=./Test-Data/Products.db");
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

public class BuildSelectStatement_Tests
{

  [Fact]
  public void BuildSelectStatement_MsSql()
  {
    // Arrange
    MsSqlDialectBuilder dialect = new MsSqlDialectBuilder(TestSampleData.DB);

    // Act
    string select = dialect.BuildSelectClause("*", "pRoDuCtS");

    // Assert
    Assert.Equal("SELECT * FROM [Shopping].[Products] AS [T_Products]", select);
  }

  [Fact]
  public void BuildSelectStatement_MsSql_WithFields()
  {
    // Arrange
    MsSqlDialectBuilder dialect = new MsSqlDialectBuilder(TestSampleData.DB);

    // Act
    string select = dialect.BuildSelectClause("name,price", "pRoDuCtS");

    // Assert
    Assert.Equal("SELECT [T_Products].[Name], [T_Products].[Price] FROM [Shopping].[Products] AS [T_Products]", select);
  }

  [Fact]
  public void BuildSelectStatement_MySql()
  {
    // Arrange
    MySqlDialectBuilder dialect = new MySqlDialectBuilder(TestSampleData.DB);

    // Act
    string select = dialect.BuildSelectClause("*", "pRoDuCtS");

    // Assert
    Assert.Equal("SELECT * FROM shopping.products AS t_products", select);
  }

  [Fact]
  public void BuildSelectStatement_MySql_WithFields()
  {
    // Arrange
    MySqlDialectBuilder dialect = new MySqlDialectBuilder(TestSampleData.DB);

    // Act
    string select = dialect.BuildSelectClause("name,price", "pRoDuCtS");

    // Assert
    Assert.Equal("SELECT t_products.name, t_products.price FROM shopping.products AS t_products", select);
  }

  [Fact]
  public void BuildSelectStatement_PostgreSql()
  {
    // Arrange
    PostgreSqlDialectBuilder dialect = new PostgreSqlDialectBuilder(TestSampleData.DB);

    // Act
    string select = dialect.BuildSelectClause("*", "pRoDuCtS");

    // Assert
    Assert.Equal("SELECT * FROM shopping.products AS t_products", select);
  }

  [Fact]
  public void BuildSelectStatement_PostgreSql_WithFields()
  {
    // Arrange
    PostgreSqlDialectBuilder dialect = new PostgreSqlDialectBuilder(TestSampleData.DB);

    // Act
    string select = dialect.BuildSelectClause("naMe,pRice", "pRoDuCtS");

    // Assert
    Assert.Equal("SELECT t_products.name, t_products.price FROM shopping.products AS t_products", select);
  }
}
