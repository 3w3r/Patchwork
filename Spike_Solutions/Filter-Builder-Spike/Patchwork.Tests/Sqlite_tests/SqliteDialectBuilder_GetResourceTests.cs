using Dapper;
using Patchwork.Api;
using Patchwork.SqlDialects.Sqlite;
using Microsoft.Data.Sqlite;

namespace Patchwork.Tests.Sqlite_tests;

public class SqliteDialectBuilder_GetResourceTests
{
  [Fact]
  public void BuildGetSingleSql_ShouldBuildSelectStatement_IncludeParentTable()
  {
    // Arrange
    SqliteDialectBuilder sut = new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString());

    // Act
    var sql = sut.BuildGetSingleSql("dbo", "Products", "S24_1937", "*", "productlines");

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("SELECT *", sql.Sql);
    Assert.Contains("FROM products", sql.Sql);

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
    SqliteDialectBuilder sut = new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString());

    // Act
    var sql = sut.BuildGetSingleSql("dbo", "Products", "S24_1937", "*", "orderdetails");

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("SELECT *", sql.Sql);
    Assert.Contains("FROM products", sql.Sql);

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
    SqliteDialectBuilder sut = new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString());

    // Act
    var sql = sut.BuildGetSingleSql("dbo", "Products", "S24_1937", "*", "orderdetails,orders");

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("SELECT *", sql.Sql);
    Assert.Contains("FROM products", sql.Sql);

    using var connect = sut.GetWriterConnection();

    var found = connect.Connection.QueryFirst(sql.Sql, sql.Parameters, connect.Transaction);
    Assert.Equal("S24_1937", found.productCode);
    Assert.Equal("1939 Chevrolet Deluxe Coupe", found.productName);
    Assert.Equal("Vintage Cars", found.productLine);
    Assert.Equal("1:24", found.productScale);
    Assert.True(found.quantityOrdered > 0);
    Assert.False(string.IsNullOrEmpty(found.status));
  }

  [Fact]
  public void BuildGetSingleSql_ValidateDapperSupportsSqlite()
  {
    // Arrange
    using var connect = new SqliteConnection(ConnectionStringManager.GetSqliteConnectionString());
    connect.Open();
    var sql = "SELECT t_orders.orderNumber, t_orders.shippedDate, t_orders.status " +
      "FROM orders AS t_orders " +
      "WHERE t_orders.status = @V0 COLLATE NOCASE " +
      "LIMIT 10 OFFSET 0;";
    var p1 = new { V0 = "shipped" };
    var p2 = new Dictionary<string, object>();
    p2.Add("V0", "shipped");
    var p3 = new DynamicParameters();
    p3.Add("V0", "shipped");

    // Act
    var found1 = connect.QueryFirstOrDefault<dynamic>(sql, p1);
    var found2 = connect.QueryFirstOrDefault<dynamic>(sql, p2);
    var found3 = connect.QueryFirstOrDefault<dynamic>(sql, p3);

    // Assert
    Assert.NotNull(found1);
    Assert.NotNull(found2);
    Assert.NotNull(found3);

    Assert.Equal("Shipped", found1!.status);
    Assert.Equal("Shipped", found2!.status);
    Assert.Equal("Shipped", found3!.status);
  }
}
