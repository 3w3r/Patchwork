using Patchwork.SqlDialects;

namespace Patchwork.Tests;

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
