using Patchwork.SqlDialects;

namespace Patchwork.Tests;
public class BuildSelectStatement_Tests
{

  [Fact]
  public void BuildSelectStatement_MsSql()
  {
    // Arrange
    var dialect = new MsSqlDialectBuilder(TestSampleData.DB);

    // Act
    var select = dialect.BuildSelectClause("*", "pRoDuCtS");

    // Assert
    Assert.Equal("SELECT * FROM [Shopping].[Products] AS [T_Products]", select);
  }

  [Fact]
  public void BuildSelectStatement_MsSql_WithFields()
  {
    // Arrange
    var dialect = new MsSqlDialectBuilder(TestSampleData.DB);

    // Act
    var select = dialect.BuildSelectClause("name,price", "pRoDuCtS");

    // Assert
    Assert.Equal("SELECT [T_Products].[Name], [T_Products].[Price] FROM [Shopping].[Products] AS [T_Products]", select);
  }

  [Fact]
  public void BuildSelectStatement_MySql()
  {
    // Arrange
    var dialect = new MySqlDialectBuilder(TestSampleData.DB);

    // Act
    var select = dialect.BuildSelectClause("*", "pRoDuCtS");

    // Assert
    Assert.Equal("SELECT * FROM shopping.products AS t_products", select);
  }

  [Fact]
  public void BuildSelectStatement_MySql_WithFields()
  {
    // Arrange
    var dialect = new MySqlDialectBuilder(TestSampleData.DB);

    // Act
    var select = dialect.BuildSelectClause("name,price", "pRoDuCtS");

    // Assert
    Assert.Equal("SELECT t_products.name, t_products.price FROM shopping.products AS t_products", select);
  }

  [Fact]
  public void BuildSelectStatement_PostgreSql()
  {
    // Arrange
    var dialect = new PostgreSqlDialectBuilder(TestSampleData.DB);

    // Act
    var select = dialect.BuildSelectClause("*", "pRoDuCtS");

    // Assert
    Assert.Equal("SELECT * FROM shopping.products AS t_products", select);
  }

  [Fact]
  public void BuildSelectStatement_PostgreSql_WithFields()
  {
    // Arrange
    var dialect = new PostgreSqlDialectBuilder(TestSampleData.DB);

    // Act
    var select = dialect.BuildSelectClause("name,price", "pRoDuCtS");

    // Assert
    Assert.Equal("SELECT t_products.name, t_products.price FROM shopping.products AS t_products", select);
  }
}
