using MySqlConnector;
using Patchwork.SqlDialects;
using Dapper;

namespace Patchwork.Tests;

public class MySqlDialectBuilderTests
{
  [Fact]
  public void BuildGetListSql_ShouldBuildSelectStatement_ForGetListEndpoint()
  {
    // Arrange
    MySqlDialectBuilder sut = new MySqlDialectBuilder(ConnectionStringManager.GetMySqlConnectionString());

    // Act
    var sql = sut.BuildGetListSql("Taskboard", "Products", "*", "productName sw 197", "productName", 10, 0);

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("SELECT *", sql.Sql);
    Assert.Contains("FROM taskboard.products", sql.Sql);
    Assert.Contains("WHERE t_products.productname LIKE '197%'", sql.Sql);
    Assert.Contains("LIMIT 10", sql.Sql);
    Assert.Contains("OFFSET 0", sql.Sql);

    using var connect = new MySqlConnection(ConnectionStringManager.GetMySqlConnectionString());
    connect.Open();

    var found = connect.Query(sql.Sql, sql.Parameters);
    Assert.Equal(8, found.Count());
    foreach (var item in found)
    {
      Assert.StartsWith("197", item.productName);
    }

    connect.Clone();
  }

  [Fact]
  public void BuildGetListSql_ShouldBuildSelectStatement_ForGetListEndpointWithOffset()
  {
    // Arrange
    MySqlDialectBuilder sut = new MySqlDialectBuilder(ConnectionStringManager.GetMySqlConnectionString());

    // Act
    var sql1 = sut.BuildGetListSql("Taskboard", "Orders", "orderNumber, shippedDate, status", "Status eq 'shipped'", "", 10, 0);
    var sql2 = sut.BuildGetListSql("Taskboard", "Orders", "orderNumber, shippedDate, status", "Status eq 'shipped'", "", 5, 5);

    // Assert
    Assert.NotEmpty(sql1.Sql);
    Assert.Contains("SELECT t_orders.ordernumber, t_orders.shippeddate, t_orders.status", sql1.Sql);
    Assert.Contains("FROM taskboard.orders", sql1.Sql);
    Assert.Contains("WHERE t_orders.status = 'shipped'", sql1.Sql);
    Assert.Contains("LIMIT 10", sql1.Sql);
    Assert.Contains("OFFSET 0", sql1.Sql);

    Assert.NotEmpty(sql2.Sql);
    Assert.Contains("SELECT t_orders.ordernumber, t_orders.shippeddate, t_orders.status", sql2.Sql);
    Assert.Contains("FROM taskboard.orders", sql2.Sql);
    Assert.Contains("WHERE t_orders.status = 'shipped'", sql2.Sql);
    Assert.Contains("LIMIT 5", sql2.Sql);
    Assert.Contains("OFFSET 5", sql2.Sql);

    using var connect = new MySqlConnection(ConnectionStringManager.GetMySqlConnectionString());
    connect.Open();

    var found1 = connect.Query(sql1.Sql, sql1.Parameters).ToArray();
    var found2 = connect.Query(sql2.Sql, sql2.Parameters).ToArray();

    Assert.Equal(10, found1.Length);
    Assert.Equal(5, found2.Length);
    foreach (var item in found1)
    {
      Assert.Equal("Shipped", item.status);
    }

    for (int i = 0; i < found2.Length; i++)
    {
      Assert.Equal(found2[i].ordernumber.ToString(), found1[i + 5].ordernumber.ToString());
      Assert.Equal(found2[i].shippeddate.ToString(), found1[i + 5].shippeddate.ToString());
      Assert.Equal(found2[i].status.ToString(), found1[i + 5].status.ToString());
    }

    connect.Clone();
  }

  [Fact]
  public void BuildGetListSql_ThrowsArgumentException_WhenSchemaNameIsNull()
  {
    // Arrange
    MySqlDialectBuilder sut = new MySqlDialectBuilder(ConnectionStringManager.GetMySqlConnectionString());

    // Act
    ArgumentException ex = Assert.Throws<ArgumentException>(() =>
    {
      sut.BuildGetListSql(null, "Products");
    });

    ArgumentException ex1 = Assert.Throws<ArgumentException>(() =>
    {
      sut.BuildGetListSql("", "Products");
    });

    // Assert
    Assert.StartsWith("Schema name is required.", ex.Message);
    Assert.StartsWith("Schema name is required.", ex1.Message);
  }

  [Fact]
  public void BuildGetListSql_ThrowsArgumentException_WhenEntityNameIsNull()
  {
    // Arrange
    MySqlDialectBuilder sut = new MySqlDialectBuilder(ConnectionStringManager.GetMySqlConnectionString());

    // Act
    ArgumentException ex = Assert.Throws<ArgumentException>(() =>
    {
      sut.BuildGetListSql("Shopping", null);
    });
    ArgumentException ex1 = Assert.Throws<ArgumentException>(() =>
    {
      sut.BuildGetListSql("Shopping", "");
    });

    // Assert
    Assert.StartsWith("Entity name is required.", ex.Message);
    Assert.StartsWith("Entity name is required.", ex1.Message);
  }

  [Fact]
  public void BuildGetSingleSql_ShouldBuildSelectStatement_ForGetSingleendpoint()
  {
    // Arrange
    MySqlDialectBuilder sut = new MySqlDialectBuilder(ConnectionStringManager.GetMySqlConnectionString());

    // Act
    var sql = sut.BuildGetSingleSql("Taskboard", "Products", "S24_1937", "*", "productlines");

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("SELECT *", sql.Sql);
    Assert.Contains("FROM taskboard.products", sql.Sql);

    using var connect = new MySqlConnection(ConnectionStringManager.GetMySqlConnectionString());
    connect.Open();
    var found = connect.QueryFirst(sql.Sql, sql.Parameters);
    Assert.Equal("S24_1937", found.productCode);
    Assert.Equal("1939 Chevrolet Deluxe Coupe", found.productName);
    Assert.Equal("Vintage Cars", found.productLine);
    Assert.Equal("1:24", found.productScale);
    Assert.False(string.IsNullOrEmpty(found.textDescription));

    connect.Clone();
  }
}
