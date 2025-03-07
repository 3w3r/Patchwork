using Dapper;
using Patchwork.SqlDialects.MySql;

namespace Patchwork.Tests.MySql_tests;
public class MySqlDialectBuilder_GetListTests
{
  [Fact]
  public void BuildGetListSql_ShouldBuildSelectStatement_ForGetListEndpointWithStartsWith()
  {
    // Arrange
    MySqlDialectBuilder sut = new MySqlDialectBuilder(ConnectionStringManager.GetMySqlConnectionString());

    // Act
    var sql = sut.BuildGetListSql("Taskboard", "Products", "*", "productName sw '197'", "productName", 10, 0);

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("SELECT *", sql.Sql);
    Assert.Contains("FROM taskboard.products", sql.Sql);
    Assert.Contains("WHERE t_products.productName LIKE @V0", sql.Sql);
    Assert.Contains("LIMIT 10", sql.Sql);
    Assert.Contains("OFFSET 0", sql.Sql);
    Assert.Equal("197%", sql.Parameters.First().Value);

    using var connect = sut.GetWriterConnection();

    var found = connect.Connection.Query(sql.Sql, sql.Parameters, connect.Transaction);
    Assert.Equal(8, found.Count());
    foreach (var item in found)
    {
      Assert.StartsWith("197", item.productName);
    }
  }

  [Fact]
  public void BuildGetListSql_ShouldBuildSelectStatement_ForGetListEndpointWithContains()
  {
    // Arrange
    MySqlDialectBuilder sut = new MySqlDialectBuilder(ConnectionStringManager.GetMySqlConnectionString());

    // Act
    var sql = sut.BuildGetListSql("Taskboard", "Products", "*", "productName ct 'Chevy'", "productName", 10, 0);

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("SELECT *", sql.Sql);
    Assert.Contains("FROM taskboard.products", sql.Sql);
    Assert.Contains("WHERE t_products.productName LIKE @V0", sql.Sql);
    Assert.Contains("LIMIT 10", sql.Sql);
    Assert.Contains("OFFSET 0", sql.Sql);
    Assert.Equal("%Chevy%", sql.Parameters.First().Value);

    using var connect = sut.GetWriterConnection();

    var found = connect.Connection.Query(sql.Sql, sql.Parameters, connect.Transaction);
    Assert.Equal(4, found.Count());
    foreach (var item in found)
    {
      Assert.Contains("Chevy", item.productName);
    }
  }

  [Fact]
  public void BuildGetListSql_ShouldBuildSelectStatement_ForGetListWithLongFilter()
  {
    // Arrange
    MySqlDialectBuilder sut = new MySqlDialectBuilder(ConnectionStringManager.GetMySqlConnectionString());

    // Act
    var sql = sut.BuildGetListSql("Taskboard", "Products", "*",
      "productName ct 'Chevy' OR productName sw '1978' OR (ProductName ct 'Alpine') OR productName ct 'Roadster' OR productName ct 'Benz' " +
      "OR productName ct 'Moto' OR productName ct 'Pickup' OR (pRoductName ct 'Hawk' AND productName ct 'Black') OR productName ct 'Ford' " +
      "OR productName ct 'Hemi' OR productName ct 'Honda' OR produCtName sw '1952' ",
      "productName", 20, 5);

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("SELECT *", sql.Sql);
    Assert.Contains("FROM taskboard.products", sql.Sql);
    Assert.Contains("t_products.productName LIKE @V0", sql.Sql);
    Assert.Contains("t_products.productName LIKE @V1", sql.Sql);
    Assert.Contains("t_products.productName LIKE @V2", sql.Sql);
    Assert.Contains("t_products.productName LIKE @V3", sql.Sql);
    Assert.Contains("t_products.productName LIKE @V4", sql.Sql);
    Assert.Contains("t_products.productName LIKE @V5", sql.Sql);
    Assert.Contains("t_products.productName LIKE @V6", sql.Sql);
    Assert.Contains("t_products.productName LIKE @V7", sql.Sql);
    Assert.Contains("t_products.productName LIKE @V8", sql.Sql);
    Assert.Contains("t_products.productName LIKE @V9", sql.Sql);
    Assert.Contains("t_products.productName LIKE @V10", sql.Sql);
    Assert.Contains("t_products.productName LIKE @V11", sql.Sql);
    Assert.Contains("t_products.productName LIKE @V12", sql.Sql);
    Assert.Contains("LIMIT 20", sql.Sql);
    Assert.Contains("OFFSET 5", sql.Sql);
    Assert.Equal("%Chevy%", sql.Parameters.First().Value);
    Assert.Equal("1952%", sql.Parameters.Last().Value);

    using var connect = sut.GetWriterConnection();

    var found = connect.Connection.Query(sql.Sql, sql.Parameters, connect.Transaction);
    Assert.Equal(20, found.Count());
  }

  [Fact]
  public void BuildGetListSql_ShouldBuildSelectStatement_ForGetListEndpointWithOffset()
  {
    // Arrange
    MySqlDialectBuilder sut = new MySqlDialectBuilder(ConnectionStringManager.GetMySqlConnectionString());

    // Act
    var sql1 = sut.BuildGetListSql("Taskboard", "Orders", "orderNumber, shippedDate, status", "StAtUs eq 'shipped'", "", 10, 0);
    var sql2 = sut.BuildGetListSql("Taskboard", "Orders", "orderNumber, shippedDate, status", "Status eq 'shipped'", "", 5, 5);

    // Assert
    Assert.NotEmpty(sql1.Sql);
    Assert.Contains("SELECT t_orders.orderNumber, t_orders.shippedDate, t_orders.status", sql1.Sql);
    Assert.Contains("FROM taskboard.orders", sql1.Sql);
    Assert.Contains("WHERE t_orders.status = @V0", sql1.Sql);
    Assert.Contains("LIMIT 10", sql1.Sql);
    Assert.Contains("OFFSET 0", sql1.Sql);
    Assert.Equal("shipped", sql1.Parameters.First().Value);

    Assert.NotEmpty(sql2.Sql);
    Assert.Contains("SELECT t_orders.orderNumber, t_orders.shippedDate, t_orders.status", sql2.Sql);
    Assert.Contains("FROM taskboard.orders", sql2.Sql);
    Assert.Contains("WHERE t_orders.status = @V0", sql2.Sql);
    Assert.Contains("LIMIT 5", sql2.Sql);
    Assert.Contains("OFFSET 5", sql2.Sql);
    Assert.Equal("shipped", sql2.Parameters.First().Value);

    using var connect = sut.GetWriterConnection();

    var found1 = connect.Connection.Query(sql1.Sql, sql1.Parameters, connect.Transaction).ToArray();
    var found2 = connect.Connection.Query(sql2.Sql, sql2.Parameters, connect.Transaction).ToArray();

    Assert.Equal(10, found1.Length);
    Assert.Equal(5, found2.Length);
    foreach (var item in found1)
    {
      Assert.Equal("Shipped", item.status);
    }

    for (int i = 0; i < found2.Length; i++)
    {
      Assert.Equal(found2[i].orderNumber.ToString(), found1[i + 5].orderNumber.ToString());
      Assert.Equal(found2[i].shippedDate.ToString(), found1[i + 5].shippedDate.ToString());
      Assert.Equal(found2[i].status.ToString(), found1[i + 5].status.ToString());
    }
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
}
