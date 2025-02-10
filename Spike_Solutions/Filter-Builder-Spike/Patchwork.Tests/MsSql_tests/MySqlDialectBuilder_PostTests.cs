﻿using System.Data.Common;
using Dapper;
using Patchwork.SqlDialects.MsSql;
using Patchwork.SqlStatements;

namespace Patchwork.Tests.MsSql_tests;

public class MsSqlDialectBuilder_PostTests
{
  private readonly string cageJson = "{ \n" +
"  \"employeeNumber\": \"9999\", \n" +
"  \"lastName\": \"Cage\", \n" +
"  \"firstName\": \"Johnny\", \n" +
"  \"extension\": \"x99\", \n" +
"  \"email\": \"jcage@classicmodelcars.com\", \n" +
"  \"officeCode\": \"5\", \n" +
"  \"reportsTo\": \"1621\", \n" +
"  \"jobTitle\": \"Sales Rep\" \n" +
"}";
  [Fact]
  public void BuildPostSql_ShouldInsertResource()
  {
    // Arrange
    var sut = new MsSqlDialectBuilder(ConnectionStringManager.GetMsSqlConnectionString());

    // Act
    InsertStatement sql = sut.BuildPostSingleSql("classicmodels", "employees", cageJson);
    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("INSERT INTO [classicmodels].[employees]", sql.Sql);
    Assert.Contains("VALUES (", sql.Sql);
    Assert.DoesNotContain("employeeNumber", sql.Sql);
    Assert.Contains("[lastName]", sql.Sql);
    Assert.Contains("[firstName]", sql.Sql);
    Assert.Contains("[email]", sql.Sql);
    Assert.Contains("[extension]", sql.Sql);
    Assert.Contains("@lastName", sql.Sql);
    Assert.Contains("@firstName", sql.Sql);
    Assert.Contains("@email", sql.Sql);
    Assert.Contains("@extension", sql.Sql);

    using DbConnection connect = sut.GetConnection();
    connect.Open();
    using var transaction = connect.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);
    
    try
    {
      int changeCount = connect.Execute(sql.Sql, sql.Parameters, transaction);
      dynamic found = connect.QueryFirst("SELECT * FROM [classicmodels].[employees] WHERE [lastName] = @lastName AND [firstName] = @firstName", sql.Parameters, transaction);

      Assert.Equal(1, changeCount);
      Assert.Equal("Cage", found.lastName);
      Assert.Equal("x99", found.extension);
    }
    finally
    {
      transaction.Rollback();
      connect.Close();
    }
  }
}
