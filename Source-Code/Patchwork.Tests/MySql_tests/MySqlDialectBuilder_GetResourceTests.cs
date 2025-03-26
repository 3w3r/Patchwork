using Dapper;

using Patchwork.SqlDialects;
using Patchwork.SqlDialects.MySql;

namespace Patchwork.Tests.MySql_tests;

public class MySqlDialectBuilder_GetResourceTests
{
  [SkippableFact, Trait("Category", "LocalOnly")]
  public void BuildGetSingleSql_ShouldBuildSelectStatement_IncludeParentTable()
  {
    // Arrange
    var connectionstring = string.Empty;
    try
    { connectionstring = ConnectionStringManager.GetMySqlConnectionString(); } catch { }
    Skip.If(string.IsNullOrEmpty(connectionstring));

    ISqlDialectBuilder sut = new MySqlDialectBuilder(connectionstring);
    try
    { sut.DiscoverSchema(); } catch { Skip.If(true, "Database schema discovery failed"); }

    // Act
    var sql = sut.BuildGetSingleSql("classicmodels", "Products", "S24_1937", "*", "productlines");

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("SELECT *", sql.Sql);
    Assert.Contains("FROM classicmodels.products", sql.Sql);

    using var connect = sut.GetWriterConnection();

    var found = connect.Connection.QueryFirst(sql.Sql, sql.Parameters, connect.Transaction);
    Assert.Equal("S24_1937", found.productCode);
    Assert.Equal("1939 Chevrolet Deluxe Coupe", found.productName);
    Assert.Equal("Vintage Cars", found.productLine);
    Assert.Equal("1:24", found.productScale);
    Assert.False(string.IsNullOrEmpty(found.textDescription));
  }

  [SkippableFact, Trait("Category", "LocalOnly")]
  public void BuildGetSingleSql_ShouldBuildSelectStatement_IncludeChildCollection()
  {
    // Arrange
    var connectionstring = string.Empty;
    try
    { connectionstring = ConnectionStringManager.GetMySqlConnectionString(); } catch { }
    Skip.If(string.IsNullOrEmpty(connectionstring));

    ISqlDialectBuilder sut = new MySqlDialectBuilder(connectionstring);
    try
    { sut.DiscoverSchema(); } catch { Skip.If(true, "Database schema discovery failed"); }

    // Act
    var sql = sut.BuildGetSingleSql("classicmodels", "Products", "S24_1937", "*", "orderdetails");

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("SELECT *", sql.Sql);
    Assert.Contains("FROM classicmodels.products", sql.Sql);

    using var connect = sut.GetWriterConnection();

    var found = connect.Connection.QueryFirst(sql.Sql, sql.Parameters, connect.Transaction);
    Assert.Equal("S24_1937", found.productCode);
    Assert.Equal("1939 Chevrolet Deluxe Coupe", found.productName);
    Assert.Equal("Vintage Cars", found.productLine);
    Assert.Equal("1:24", found.productScale);
    Assert.True(found.quantityOrdered > 0);
  }

  [SkippableFact, Trait("Category", "LocalOnly")]
  public void BuildGetSingleSql_ShouldBuildSelectStatement_IncludeChildRelationshipChain()
  {
    // Arrange
    var connectionstring = string.Empty;
    try
    { connectionstring = ConnectionStringManager.GetMySqlConnectionString(); } catch { }
    Skip.If(string.IsNullOrEmpty(connectionstring));

    ISqlDialectBuilder sut = new MySqlDialectBuilder(connectionstring);
    try
    { sut.DiscoverSchema(); } catch { Skip.If(true, "Database schema discovery failed"); }

    // Act
    var sql = sut.BuildGetSingleSql("classicmodels", "Products", "S24_1937", "*", "orderdetails,orders");

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("SELECT *", sql.Sql);
    Assert.Contains("FROM classicmodels.products", sql.Sql);

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
