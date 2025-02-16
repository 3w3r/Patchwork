﻿using Patchwork.SqlDialects;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Patchwork.Tests;

public class MsSqlDialectBuilderTests
{
  [Fact]
  public void BuildGetListSql_ShouldBuildSelectStatement_ForGetListWithLongFilter()
  {
    // Arrange
    MsSqlDialectBuilder sut = new MsSqlDialectBuilder(ConnectionStringManager.GetMsSqlConnectionString());

    // Act
    var sql = sut.BuildGetListSql("public", "products", "*",
      "productName ct 'Chevy' OR productName sw '1978' OR (productName ct 'Alpine') OR productName ct 'Roadster' OR productName ct 'Benz' " +
      "OR productName ct 'Moto' OR productName ct 'Pickup' OR (productName ct 'Hawk' AND productName ct 'Black') OR productName ct 'Ford' " +
      "OR productName ct 'Hemi' OR productName ct 'Honda' OR productName sw '1952' ",
      "productName", 20, 5);

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("SELECT *", sql.Sql);
    Assert.Contains("FROM [classicmodels].[products]", sql.Sql);
    Assert.Contains("[T_products].[productName] LIKE @V0", sql.Sql);
    Assert.Contains("[T_products].[productName] LIKE @V1", sql.Sql);
    Assert.Contains("[T_products].[productName] LIKE @V2", sql.Sql);
    Assert.Contains("[T_products].[productName] LIKE @V3", sql.Sql);
    Assert.Contains("[T_products].[productName] LIKE @V4", sql.Sql);
    Assert.Contains("[T_products].[productName] LIKE @V5", sql.Sql);
    Assert.Contains("[T_products].[productName] LIKE @V6", sql.Sql);
    Assert.Contains("[T_products].[productName] LIKE @V7", sql.Sql);
    Assert.Contains("[T_products].[productName] LIKE @V8", sql.Sql);
    Assert.Contains("[T_products].[productName] LIKE @V9", sql.Sql);
    Assert.Contains("[T_products].[productName] LIKE @V10", sql.Sql);
    Assert.Contains("[T_products].[productName] LIKE @V11", sql.Sql);
    Assert.Contains("[T_products].[productName] LIKE @V12", sql.Sql);
    Assert.Contains("FETCH NEXT 20 ROWS ONLY", sql.Sql);
    Assert.Contains("OFFSET 5", sql.Sql);
    Assert.Equal("%Chevy%", sql.Parameters.First().Value);
    Assert.Equal("1952%", sql.Parameters.Last().Value);

    using var connect = new SqlConnection(ConnectionStringManager.GetMsSqlConnectionString());
    connect.Open();

    var found = connect.Query(sql.Sql, sql.Parameters);
    Assert.Equal(20, found.Count());

    connect.Close();
  }
}
