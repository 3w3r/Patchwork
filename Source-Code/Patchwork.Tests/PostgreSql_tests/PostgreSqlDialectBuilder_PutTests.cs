using System.Text.Json;
using Dapper;
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

  [SkippableFact, Trait("Category", "LocalOnly")]
  public void BuildPutSql_ShouldUpdateResource_WhenJsonIsChanged()
  {
    // Arrange
    var connectionstring = string.Empty;
    try
    { connectionstring = ConnectionStringManager.GetPostgreSqlConnectionString(); } catch { }
    Skip.If(string.IsNullOrEmpty(connectionstring));

    ISqlDialectBuilder sut = new PostgreSqlDialectBuilder(connectionstring);
    try
    { sut.DiscoverSchema(); } catch { Skip.If(true, "Database schema discovery failed"); }

    // Act
    SqlStatements.UpdateStatement sql = sut.BuildPutSingleSql("classicmodels", "employees", "1625", katoJsonUpdate);

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("UPDATE classicmodels.employees", sql.Sql);
    Assert.Contains("SET", sql.Sql);
    Assert.DoesNotContain("employeenumber = @employeeNumber", sql.Sql);
    Assert.Contains("lastname = @lastName", sql.Sql);
    Assert.Contains("firstname = @firstName", sql.Sql);
    Assert.Contains("email = @email", sql.Sql);
    Assert.Contains("extension = @extension", sql.Sql);

    using var connect = sut.GetWriterConnection();

    try
    {
      int changeCount = connect.Connection.Execute(sql.Sql, sql.Parameters, connect.Transaction);
      dynamic found = connect.Connection.QueryFirst("SELECT * FROM classicmodels.employees WHERE employeenumber = @id", sql.Parameters, connect.Transaction);

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
