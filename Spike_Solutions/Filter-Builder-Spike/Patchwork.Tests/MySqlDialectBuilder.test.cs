using MySqlConnector;
using Patchwork.SqlDialects;
using Dapper;
using System.Dynamic;

namespace Patchwork.Tests;

public class MySqlDialectBuilderTests
{
  private static string Conn = "Server=mysql.markewer.com;User ID=seth_ewer;Password=a0edfca687e487391bee7f18f3349b;Database=taskboard;";
  [Fact]
  public void BuildGetListSql_ShouldBuildSelectStatement_ForGetListEndpoint()
  {
    // Arrange
    MySqlDialectBuilder sut = new MySqlDialectBuilder(Conn);

    // Act
    string sql = sut.BuildGetListSql("Taskboard", "Products", "*", "productName sw 197", "productName", 10, 0);

    // Assert
    Assert.NotEmpty(sql);
    Assert.Contains("SELECT *", sql);
    Assert.Contains("FROM taskboard.products", sql);
    Assert.Contains("WHERE t_products.productname LIKE '197%'", sql);
    Assert.Contains("LIMIT 10", sql);
    Assert.Contains("OFFSET 0", sql);

    using var connect = new MySqlConnection(Conn);
    connect.Open();

    var found = connect.Query(sql);
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
    MySqlDialectBuilder sut = new MySqlDialectBuilder(Conn);

    // Act
    string sql1 = sut.BuildGetListSql("Taskboard", "Orders", "orderNumber, shippedDate, status", "Status eq 'shipped'", "", 10, 0);
    string sql2 = sut.BuildGetListSql("Taskboard", "Orders", "orderNumber, shippedDate, status", "Status eq 'shipped'", "", 5, 5);

    // Assert
    Assert.NotEmpty(sql1);
    Assert.Contains("SELECT t_orders.ordernumber, t_orders.shippeddate, t_orders.status", sql1);
    Assert.Contains("FROM taskboard.orders", sql1);
    Assert.Contains("WHERE t_orders.status = 'shipped'", sql1);
    Assert.Contains("LIMIT 10", sql1);
    Assert.Contains("OFFSET 0", sql1);

    Assert.NotEmpty(sql2);
    Assert.Contains("SELECT t_orders.ordernumber, t_orders.shippeddate, t_orders.status", sql2);
    Assert.Contains("FROM taskboard.orders", sql2);
    Assert.Contains("WHERE t_orders.status = 'shipped'", sql2);
    Assert.Contains("LIMIT 5", sql2);
    Assert.Contains("OFFSET 5", sql2);

    using var connect = new MySqlConnection(Conn);
    connect.Open();

    var found1 = connect.Query(sql1).ToArray();
    var found2 = connect.Query(sql2).ToArray();

    Assert.Equal(10, found1.Length);
    Assert.Equal( 5, found2.Length);
    foreach (var item in found1)
    {
      Assert.Equal("Shipped", item.status);
    }

    for(int i= 0; i < found2.Length; i++)
    {
      Assert.Equal(found2[i].ordernumber.ToString(), found1[i+5].ordernumber.ToString());
      Assert.Equal(found2[i].shippeddate.ToString(), found1[i+5].shippeddate.ToString());
      Assert.Equal(found2[i].status.ToString(), found1[i+5].status.ToString());
    }

    connect.Clone();
  }

  [Fact]
  public void BuildGetListSql_ThrowsArgumentException_WhenSchemaNameIsNull()
  {
    // Arrange
    MySqlDialectBuilder sut = new MySqlDialectBuilder(Conn);

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
    MySqlDialectBuilder sut = new MySqlDialectBuilder(Conn);

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
}