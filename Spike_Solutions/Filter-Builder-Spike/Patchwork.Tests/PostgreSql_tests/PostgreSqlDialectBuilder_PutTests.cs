using System.Text.Json;
using Dapper;
using Patchwork.Api;
using Patchwork.SqlDialects;
using Patchwork.SqlDialects.PostgreSql;

namespace Patchwork.Tests.PostgreSql_tests;

public class PostgreSqlDialectBuilder_PutTests
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
    ISqlDialectBuilder sut = new PostgreSqlDialectBuilder(ConnectionStringManager.GetPostgreSqlConnectionString());

    // Act
    SqlStatements.UpdateStatement sql = sut.BuildPutSingleSql("public", "employees", "1625", katoJsonUpdate);

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("UPDATE public.employees", sql.Sql);
    Assert.Contains("SET", sql.Sql);
    Assert.DoesNotContain("employeenumber = @employeeNumber", sql.Sql);
    Assert.Contains("lastname = @lastName", sql.Sql);
    Assert.Contains("firstname = @firstName", sql.Sql);
    Assert.Contains("email = @email", sql.Sql);
    Assert.Contains("extension = @extension", sql.Sql);

    using var connect = sut.GetConnection();

    try
    {
      int changeCount = connect.Connection.Execute(sql.Sql, sql.Parameters, connect.Transaction);
      dynamic found = connect.Connection.QueryFirst("SELECT * FROM public.employees WHERE employeenumber = @id", sql.Parameters, connect.Transaction);

      Assert.Equal(1, changeCount);
      Assert.Equal("Kato", found.lastname);
      Assert.Equal("x104", found.extension);
    }
    finally
    {
      connect.Transaction.Rollback();
    }
  }
}
