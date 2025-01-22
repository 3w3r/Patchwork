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
    var select = dialect.BuildSelectClause("pRoDuCtS");

    // Assert
    Assert.Equal("SELECT * FROM [Shopping].[Products] AS [T_Products]", select);
  }
  [Fact]
  public void BuildSelectStatement_MySql()
  {
    // Arrange
    var dialect = new MySqlDialectBuilder(TestSampleData.DB);

    // Act
    var select = dialect.BuildSelectClause("pRoDuCtS");

    // Assert
    Assert.Equal("SELECT * FROM shopping.products AS t_products", select);
  }
  [Fact]
  public void BuildSelectStatement_PostgreSql()
  {
    // Arrange
    var dialect = new PostgreSqlDialectBuilder(TestSampleData.DB);

    // Act
    var select = dialect.BuildSelectClause("pRoDuCtS");

    // Assert
    Assert.Equal("SELECT * FROM shopping.products AS t_products", select);
  }
}
