﻿using Patchwork.SqlDialects;
using Dapper;
using Npgsql;

namespace Patchwork.Tests;

public class PostgreSqlDialectBuilderTests
{
  [Fact]
  public void BuildGetListSql_ShouldBuildSelectStatement_ForGetListWithLongFilter()
  {
    // Arrange
   PostgreSqlDialectBuilder sut = new PostgreSqlDialectBuilder(ConnectionStringManager.GetPostgreSqlConnectionString());

    // Act
    var sql = sut.BuildGetListSql("public", "products", "*",
      "productName ct 'Chevy' OR productName sw '1978' OR (productName ct 'Alpine') OR productName ct 'Roadster' OR productName ct 'Benz' " +
      "OR productName ct 'Moto' OR productName ct 'Pickup' OR (productName ct 'Hawk' AND productName ct 'Black') OR productName ct 'Ford' " +
      "OR productName ct 'Hemi' OR productName ct 'Honda' OR productName sw '1952' ",
      "productName", 20, 5);

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("SELECT *", sql.Sql);
    Assert.Contains("FROM public.products", sql.Sql);
    Assert.Contains("t_products.productname ILIKE @V0", sql.Sql);
    Assert.Contains("t_products.productname ILIKE @V1", sql.Sql);
    Assert.Contains("t_products.productname ILIKE @V2", sql.Sql);
    Assert.Contains("t_products.productname ILIKE @V3", sql.Sql);
    Assert.Contains("t_products.productname ILIKE @V4", sql.Sql);
    Assert.Contains("t_products.productname ILIKE @V5", sql.Sql);
    Assert.Contains("t_products.productname ILIKE @V6", sql.Sql);
    Assert.Contains("t_products.productname ILIKE @V7", sql.Sql);
    Assert.Contains("t_products.productname ILIKE @V8", sql.Sql);
    Assert.Contains("t_products.productname ILIKE @V9", sql.Sql);
    Assert.Contains("t_products.productname ILIKE @V10", sql.Sql);
    Assert.Contains("t_products.productname ILIKE @V11", sql.Sql);
    Assert.Contains("t_products.productname ILIKE @V12", sql.Sql);
    Assert.Contains("LIMIT 20", sql.Sql);
    Assert.Contains("OFFSET 5", sql.Sql);
    Assert.Equal("%Chevy%", sql.Parameters.First().Value);
    Assert.Equal("1952%", sql.Parameters.Last().Value);

    using var connect = new NpgsqlConnection(ConnectionStringManager.GetPostgreSqlConnectionString());
    connect.Open();

    var found = connect.Query(sql.Sql, sql.Parameters);
    Assert.Equal(20, found.Count());

    connect.Close();
  }
}
