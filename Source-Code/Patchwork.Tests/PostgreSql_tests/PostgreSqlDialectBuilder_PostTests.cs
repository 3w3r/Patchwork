using System.Text.Json;
using Dapper;
using Patchwork.SqlDialects.PostgreSql;
using Patchwork.SqlStatements;

namespace Patchwork.Tests.PostgreSql_tests;

public class PostgreSqlDialectBuilder_PostTests
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
  [Fact, Trait("Category", "LocalOnly")]
  public void BuildPostSql_ShouldInsertResource()
  {
    // Arrange
    var sut = new PostgreSqlDialectBuilder(ConnectionStringManager.GetPostgreSqlConnectionString());

    // Act
    InsertStatement sql = sut.BuildPostSingleSql("classicmodels", "employees", cageJson);
    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("INSERT INTO classicmodels.employees", sql.Sql);
    Assert.Contains("VALUES (", sql.Sql);
    Assert.DoesNotContain("employeenumber", sql.Sql);
    Assert.Contains("lastname", sql.Sql);
    Assert.Contains("firstname", sql.Sql);
    Assert.Contains("email", sql.Sql);
    Assert.Contains("extension", sql.Sql);
    Assert.Contains("@lastname", sql.Sql);
    Assert.Contains("@firstname", sql.Sql);
    Assert.Contains("@email", sql.Sql);
    Assert.Contains("@extension", sql.Sql);
    Assert.Contains("RETURNING *", sql.Sql);

    using var connect = sut.GetWriterConnection();

    try
    {
      IEnumerable<dynamic> changeCount = connect.Connection.Query(sql.Sql, sql.Parameters, connect.Transaction);
      var found = changeCount.First();

      Assert.Single(changeCount);
      Assert.NotNull(found);
      Assert.Equal("Cage", found.lastname);
      Assert.Equal("x99", found.extension);
    }
    finally
    {
      connect.Transaction.Rollback();
    }
  }
}
