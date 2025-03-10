using Dapper;
using Patchwork.SqlDialects.MySql;

namespace Patchwork.Tests.MySql_tests;

public class MySqlDialectBuilder_GetResourceTests
{
  [Fact]
  public void BuildGetSingleSql_ShouldBuildSelectStatement_IncludeParentTable()
  {
    // Arrange
    MySqlDialectBuilder sut = new MySqlDialectBuilder(ConnectionStringManager.GetMySqlConnectionString());

    // Act
    var sql = sut.BuildGetSingleSql("Taskboard", "Products", "S24_1937", "*", "productlines");

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("SELECT *", sql.Sql);
    Assert.Contains("FROM taskboard.products", sql.Sql);

    using var connect = sut.GetWriterConnection();

    var found = connect.Connection.QueryFirst(sql.Sql, sql.Parameters, connect.Transaction);
    Assert.Equal("S24_1937", found.productCode);
    Assert.Equal("1939 Chevrolet Deluxe Coupe", found.productName);
    Assert.Equal("Vintage Cars", found.productLine);
    Assert.Equal("1:24", found.productScale);
    Assert.False(string.IsNullOrEmpty(found.textDescription));
  }

  [Fact]
  public void BuildGetSingleSql_ShouldBuildSelectStatement_IncludeChildCollection()
  {
    // Arrange
    MySqlDialectBuilder sut = new MySqlDialectBuilder(ConnectionStringManager.GetMySqlConnectionString());

    // Act
    var sql = sut.BuildGetSingleSql("Taskboard", "Products", "S24_1937", "*", "orderdetails");

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("SELECT *", sql.Sql);
    Assert.Contains("FROM taskboard.products", sql.Sql);

    using var connect = sut.GetWriterConnection();

    var found = connect.Connection.QueryFirst(sql.Sql, sql.Parameters, connect.Transaction);
    Assert.Equal("S24_1937", found.productCode);
    Assert.Equal("1939 Chevrolet Deluxe Coupe", found.productName);
    Assert.Equal("Vintage Cars", found.productLine);
    Assert.Equal("1:24", found.productScale);
    Assert.True(found.quantityOrdered > 0);
  }

  [Fact]
  public void BuildGetSingleSql_ShouldBuildSelectStatement_IncludeChildRelationshipChain()
  {
    // Arrange
    MySqlDialectBuilder sut = new MySqlDialectBuilder(ConnectionStringManager.GetMySqlConnectionString());

    // Act
    var sql = sut.BuildGetSingleSql("Taskboard", "Products", "S24_1937", "*", "orderdetails,orders");

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("SELECT *", sql.Sql);
    Assert.Contains("FROM taskboard.products", sql.Sql);

    using var connect = sut.GetWriterConnection();

    var found = connect.Connection.QueryFirst(sql.Sql, sql.Parameters, connect.Transaction);
    Assert.Equal("S24_1937", found.productCode);
    Assert.Equal("1939 Chevrolet Deluxe Coupe", found.productName);
    Assert.Equal("Vintage Cars", found.productLine);
    Assert.Equal("1:24", found.productScale);
    Assert.True(found.quantityOrdered > 0);
    Assert.False(string.IsNullOrEmpty(found.status));
  }
}
