﻿using System.Text.Json;
using Dapper;
using Patchwork.Api;
using Patchwork.SqlDialects;
using Patchwork.SqlDialects.MySql;

namespace Patchwork.Tests.MySql_tests;

public class MySqlDialectBuilder_PutTests
{
  private readonly JsonDocument katoJsonOriginal = JsonDocument.Parse("{ \n" +
                                                                      "  \"employeeNumber\": \"1625\", \n" +
                                                                      "  \"lastName\": \"Kato\", \n" +
                                                                      "  \"firstName\": \"Yoshimi\", \n" +
                                                                      "  \"extension\": \"x102\", \n" +
                                                                      "  \"email\": \"ykato@classicmodelcars.com\", \n" +
                                                                      "  \"officeCode\": \"5\", \n" +
                                                                      "  \"reportsTo\": \"1621\", \n" +
                                                                      "  \"jobTitle\": \"Sales Rep\" \n" +
                                                                      "}");

  private readonly JsonDocument katoJsonUpdate = JsonDocument.Parse("{ \n" +
                                                                    "  \"employeeNumber\": \"1625\", \n" +
                                                                    "  \"lastName\": \"Kato\", \n" +
                                                                    "  \"firstName\": \"Yoshimi\", \n" +
                                                                    "  \"extension\": \"x104\", \n" +
                                                                    "  \"email\": \"ykato@classicmodelcars.com\", \n" +
                                                                    "  \"officeCode\": \"5\", \n" +
                                                                    "  \"reportsTo\": \"1621\", \n" +
                                                                    "  \"jobTitle\": \"Sales Rep\" \n" +
                                                                    "}");

  [Fact]
  public void BuildPutSql_ShouldUpdateResource_WhenJsonIsChanged()
  {
    // Arrange
    ISqlDialectBuilder sut = new MySqlDialectBuilder(ConnectionStringManager.GetMySqlConnectionString());

    // Act
    SqlStatements.UpdateStatement sql = sut.BuildPutSingleSql("taskboard", "employees", "1625", katoJsonUpdate);

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("UPDATE taskboard.employees", sql.Sql);
    Assert.Contains("SET", sql.Sql);
    Assert.DoesNotContain("employeeNumber = @employeeNumber", sql.Sql);
    Assert.Contains("lastName = @lastName", sql.Sql);
    Assert.Contains("firstName = @firstName", sql.Sql);
    Assert.Contains("email = @email", sql.Sql);
    Assert.Contains("extension = @extension", sql.Sql);

    using var connect = sut.GetWriterConnection();
    try
    {
      int changeCount = connect.Connection.Execute(sql.Sql, sql.Parameters, connect.Transaction);
      dynamic found = connect.Connection.QueryFirst("SELECT * FROM taskboard.employees WHERE employeeNumber = @id", sql.Parameters, connect.Transaction);

      Assert.Equal(1, changeCount);
      Assert.Equal("Kato", found.lastName);
      Assert.Equal("x104", found.extension);
    }
    finally
    {
      connect.Transaction.Rollback();
    }
  }
}
