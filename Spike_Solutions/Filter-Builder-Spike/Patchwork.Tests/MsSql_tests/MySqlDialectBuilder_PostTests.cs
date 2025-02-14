using System.Text.Json;
using Dapper;
using Patchwork.Api;
using Patchwork.SqlDialects.MsSql;
using Patchwork.SqlStatements;

namespace Patchwork.Tests.MsSql_tests;

public class MsSqlDialectBuilder_PostTests
{
  private readonly JsonDocument cageJson = JsonDocument.Parse("{ \n" +
                                                              "  \"employeeNumber\": \"9999\", \n" +
                                                              "  \"lastName\": \"Cage\", \n" +
                                                              "  \"firstName\": \"Johnny\", \n" +
                                                              "  \"extension\": \"x99\", \n" +
                                                              "  \"email\": \"jcage@classicmodelcars.com\", \n" +
                                                              "  \"officeCode\": \"5\", \n" +
                                                              "  \"reportsTo\": \"1621\", \n" +
                                                              "  \"jobTitle\": \"Sales Rep\" \n" +
                                                              "}");
  [Fact]
  public void BuildPostSql_ShouldInsertResource()
  {
    // Arrange
    var sut = new MsSqlDialectBuilder(ConnectionStringManager.GetMsSqlConnectionString(), "Taskboard");

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
    Assert.Contains("OUTPUT inserted.*", sql.Sql);

    using var connect = sut.GetConnection();
    try
    {
      int changeCount = connect.Connection.Execute(sql.Sql, sql.Parameters, connect.Transaction);
      dynamic found = connect.Connection.QueryFirst("SELECT * FROM [classicmodels].[employees] WHERE [lastName] = @lastName AND [firstName] = @firstName", sql.Parameters, connect.Transaction);

      Assert.Equal(1, changeCount);
      Assert.Equal("Cage", found.lastName);
      Assert.Equal("x99", found.extension);
    }
    finally
    {
      connect.Transaction.Rollback();
    }
  }
}
