using Dapper;
using Patchwork.SqlDialects.Sqlite;
using Microsoft.Data.Sqlite;

namespace Patchwork.Tests.Sqlite_tests;

public class SqliteDialectBuilder_GetListTests
{
  [Fact]
  public void BuildGetListSql_UsingOperator_eq()
  {
    // Arrange
    var sut = new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString());

    // Act
    var sql = sut.BuildGetListSql("dbo", "customers", "*", "customerName eq 'Atelier graphique'", "contactLastName", 100, 0);
    using var connect = new SqliteConnection(ConnectionStringManager.GetSqliteConnectionString());
    connect.Open();

    var found = connect.Query(sql.Sql, sql.Parameters);

    // Assert
    Assert.Single(found);
    connect.Close();
  }
  [Fact]
  public void BuildGetListSql_UsingOperator_ne()
  {
    // Arrange
    var sut = new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString());

    // Act
    var sql = sut.BuildGetListSql("dbo", "customers", "*", "country ne 'USA'", "contactFirstName", 100, 0);
    using var connect = new SqliteConnection(ConnectionStringManager.GetSqliteConnectionString());
    connect.Open();

    var found = connect.Query(sql.Sql, sql.Parameters);

    // Assert
    Assert.Equal(86, found.Count());
    Assert.DoesNotContain(found, x => x.country == "USA");
    connect.Close();
  }
  [Fact]
  public void BuildGetListSql_UsingOperator_gt()
  {
    // Arrange
    var sut = new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString());

    // Act
    var sql = sut.BuildGetListSql("dbo", "customers", "*", "creditLimit gt '100000'", "phone", 100, 0);
    using var connect = new SqliteConnection(ConnectionStringManager.GetSqliteConnectionString());
    connect.Open();

    var found = connect.Query(sql.Sql, sql.Parameters);

    // Assert
    Assert.Equal(25, found.Count());
    Assert.DoesNotContain(found, x => x.creditLimit <= 100000);
    connect.Close();
  }
  [Fact]
  public void BuildGetListSql_UsingOperator_ge()
  {
    // Arrange
    var sut = new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString());

    // Act
    var sql = sut.BuildGetListSql("dbo", "customers", "*", "creditLimit ge '96800'", "city:desc", 100, 0);
    using var connect = new SqliteConnection(ConnectionStringManager.GetSqliteConnectionString());
    connect.Open();

    var found = connect.Query(sql.Sql, sql.Parameters);

    // Assert
    Assert.Equal(28, found.Count());
    Assert.Contains(found, x => x.creditLimit == 96800);
    Assert.DoesNotContain(found, x => x.creditLimit < 96800);
    connect.Close();
  }
  [Fact]
  public void BuildGetListSql_UsingOperator_lt()
  {
    // Arrange
    var sut = new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString());

    // Act
    var sql = sut.BuildGetListSql("dbo", "customers", "*", "contactLastName lt 'F'", "state, postalCode:desc, city:asc", 100, 0);
    using var connect = new SqliteConnection(ConnectionStringManager.GetSqliteConnectionString());
    connect.Open();

    var found = connect.Query(sql.Sql, sql.Parameters);

    // Assert
    Assert.Equal(30, found.Count());
    Assert.DoesNotContain(found, x => string.Compare(x.contactLastName, "F") >= 0);
    connect.Close();
  }
  [Fact]
  public void BuildGetListSql_UsingOperator_le()
  {
    // Arrange
    var sut = new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString());

    // Act
    var sql = sut.BuildGetListSql("dbo", "customers", "*", "contactLastName le 'Frick'", "creditLimit", 100, 0);
    using var connect = new SqliteConnection(ConnectionStringManager.GetSqliteConnectionString());
    connect.Open();

    var found = connect.Query(sql.Sql, sql.Parameters);

    // Assert
    Assert.Equal(41, found.Count());
    Assert.DoesNotContain(found, x => string.Compare(x.contactLastName, "Frick") > 0);
    Assert.Contains(found, x => string.Equals(x.contactLastName, "Frick"));
    connect.Close();
  }
  [Fact]
  public void BuildGetListSql_UsingOperator_in()
  {
    // Arrange
    var sut = new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString());

    // Act
    var sql = sut.BuildGetListSql("dbo", "customers", "*", "postalCode in ('44000', '97562', '08022', 'S-844 67' )", "country", 100, 0);
    using var connect = new SqliteConnection(ConnectionStringManager.GetSqliteConnectionString());
    connect.Open();

    var found = connect.Query(sql.Sql, sql.Parameters);

    // Assert
    Assert.Equal(6, found.Count());
    Assert.Contains(found, x => string.Equals(x.postalCode, "44000"));
    Assert.Contains(found, x => string.Equals(x.postalCode, "97562"));
    Assert.Contains(found, x => string.Equals(x.postalCode, "08022"));
    Assert.Contains(found, x => string.Equals(x.postalCode, "S-844 67"));
    connect.Close();
  }
  [Fact]
  public void BuildGetListSql_UsingOperator_ct()
  {
    // Arrange
    var sut = new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString());

    // Act
    var sql = sut.BuildGetListSql("dbo", "customers", "*", "customerName ct '.com'", "customerNumber", 100, 0);
    using var connect = new SqliteConnection(ConnectionStringManager.GetSqliteConnectionString());
    connect.Open();

    var found = connect.Query(sql.Sql, sql.Parameters);

    // Assert
    Assert.Equal(4, found.Count());
    Assert.DoesNotContain(found, x => !x.customerName.Contains(".com"));
    connect.Close();
  }
  [Fact]
  public void BuildGetListSql_UsingOperator_sw()
  {
    // Arrange
    var sut = new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString());

    // Act
    var sql = sut.BuildGetListSql("dbo", "customers", "*", "addressLine1 sw '24'", "salesRepEmployeeNumber", 100, 0);
    using var connect = new SqliteConnection(ConnectionStringManager.GetSqliteConnectionString());
    connect.Open();

    var found = connect.Query(sql.Sql, sql.Parameters);

    // Assert
    Assert.Equal(2, found.Count());
    Assert.DoesNotContain(found, x => !x.addressLine1.StartsWith("24"));
    connect.Close();
  }

  [Fact]
  public void BuildGetListSql_ShouldBuildSelectStatement_ForGetListEndpointWithStartsWith()
  {
    // Arrange
    var sut = new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString());

    // Act
    var sql = sut.BuildGetListSql("dbo", "Products", "*", "productName sw '197'", "productName", 10, 0);

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("SELECT *", sql.Sql);
    Assert.Contains("FROM products", sql.Sql);
    Assert.Contains("WHERE t_products.productname LIKE @V0", sql.Sql);
    Assert.Contains("LIMIT 10", sql.Sql);
    Assert.Contains("OFFSET 0", sql.Sql);
    Assert.Equal("197%", sql.Parameters.First().Value);

    using var connect = new SqliteConnection(ConnectionStringManager.GetSqliteConnectionString());
    connect.Open();

    var found = connect.Query(sql.Sql, sql.Parameters);
    Assert.Equal(8, found.Count());
    foreach (var item in found)
    {
      Assert.StartsWith("197", item.productName);
    }

    connect.Close();
  }

  [Fact]
  public void BuildGetListSql_ShouldBuildSelectStatement_ForGetListEndpointWithContains()
  {
    // Arrange
    var sut = new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString());

    // Act
    var sql = sut.BuildGetListSql("dbo", "Products", "*", "productName ct 'Chevy'", "productName", 10, 0);

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("SELECT *", sql.Sql);
    Assert.Contains("FROM products", sql.Sql);
    Assert.Contains("WHERE t_products.productname LIKE @V0", sql.Sql);
    Assert.Contains("LIMIT 10", sql.Sql);
    Assert.Contains("OFFSET 0", sql.Sql);
    Assert.Equal("%Chevy%", sql.Parameters.First().Value);

    using var connect = new SqliteConnection(ConnectionStringManager.GetSqliteConnectionString());
    connect.Open();

    var found = connect.Query(sql.Sql, sql.Parameters);
    Assert.Equal(4, found.Count());
    foreach (var item in found)
    {
      Assert.Contains("Chevy", item.productName);
    }

    connect.Close();
  }

  [Fact]
  public void BuildGetListSql_ShouldBuildSelectStatement_ForGetListWithLongFilter()
  {
    // Arrange
    SqliteDialectBuilder sut = new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString());

    // Act
    var sql = sut.BuildGetListSql("dbo", "Products", "*",
      "productName ct 'Chevy' OR productName sw '1978' OR (ProductName ct 'Alpine') OR productName ct 'Roadster' OR productName ct 'Benz' " +
      "OR productName ct 'Moto' OR productName ct 'Pickup' OR (pRoductName ct 'Hawk' AND productName ct 'Black') OR productName ct 'Ford' " +
      "OR productName ct 'Hemi' OR productName ct 'Honda' OR produCtName sw '1952' ",
      "productName", 20, 5);

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("SELECT *", sql.Sql);
    Assert.Contains("FROM products", sql.Sql);
    Assert.Contains("t_products.productname LIKE @V0", sql.Sql);
    Assert.Contains("t_products.productname LIKE @V1", sql.Sql);
    Assert.Contains("t_products.productname LIKE @V2", sql.Sql);
    Assert.Contains("t_products.productname LIKE @V3", sql.Sql);
    Assert.Contains("t_products.productname LIKE @V4", sql.Sql);
    Assert.Contains("t_products.productname LIKE @V5", sql.Sql);
    Assert.Contains("t_products.productname LIKE @V6", sql.Sql);
    Assert.Contains("t_products.productname LIKE @V7", sql.Sql);
    Assert.Contains("t_products.productname LIKE @V8", sql.Sql);
    Assert.Contains("t_products.productname LIKE @V9", sql.Sql);
    Assert.Contains("t_products.productname LIKE @V10", sql.Sql);
    Assert.Contains("t_products.productname LIKE @V11", sql.Sql);
    Assert.Contains("t_products.productname LIKE @V12", sql.Sql);
    Assert.Contains("LIMIT 20", sql.Sql);
    Assert.Contains("OFFSET 5", sql.Sql);
    Assert.Equal("%Chevy%", sql.Parameters.First().Value);
    Assert.Equal("1952%", sql.Parameters.Last().Value);

    using var connect = new SqliteConnection(ConnectionStringManager.GetSqliteConnectionString());
    connect.Open();

    var found = connect.Query(sql.Sql, sql.Parameters);
    Assert.Equal(20, found.Count());

    connect.Close();
  }

  [Fact]
  public void BuildGetListSql_ShouldBuildSelectStatement_ForGetListEndpointWithOffset()
  {
    // Arrange
    SqliteDialectBuilder sut = new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString());

    // Act
    var sql1 = sut.BuildGetListSql("dbo", "Orders", "orderNumber, shippedDate, status", "Status eq 'shipped'", "", 10, 0);
    var sql2 = sut.BuildGetListSql("dbo", "Orders", "orderNumber, shippedDate, status", "Status eq 'shipped'", "", 5, 5);

    // Assert
    Assert.NotEmpty(sql1.Sql);
    Assert.Contains("SELECT t_orders.ordernumber, t_orders.shippeddate, t_orders.status", sql1.Sql);
    Assert.Contains("FROM orders", sql1.Sql);
    Assert.Contains("WHERE t_orders.status = @V0", sql1.Sql);
    Assert.Contains("LIMIT 10", sql1.Sql);
    Assert.Contains("OFFSET 0", sql1.Sql);
    Assert.Equal("shipped", sql1.Parameters.First().Value);

    Assert.NotEmpty(sql2.Sql);
    Assert.Contains("SELECT t_orders.ordernumber, t_orders.shippeddate, t_orders.status", sql2.Sql);
    Assert.Contains("FROM orders", sql2.Sql);
    Assert.Contains("WHERE t_orders.status = @V0", sql2.Sql);
    Assert.Contains("LIMIT 5", sql2.Sql);
    Assert.Contains("OFFSET 5", sql2.Sql);
    Assert.Equal("shipped", sql2.Parameters.First().Value);

    using var connect = new SqliteConnection(ConnectionStringManager.GetSqliteConnectionString());
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
      Assert.Equal(found2[i].orderNumber.ToString(), found1[i + 5].orderNumber.ToString());
      Assert.Equal(found2[i].shippedDate.ToString(), found1[i + 5].shippedDate.ToString());
      Assert.Equal(found2[i].status.ToString(), found1[i + 5].status.ToString());
    }

    connect.Close();
  }

  [Fact]
  public void BuildGetListSql_ThrowsArgumentException_WhenSchemaNameIsNull()
  {
    // Arrange
    SqliteDialectBuilder sut = new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString());

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
    SqliteDialectBuilder sut = new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString());

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
