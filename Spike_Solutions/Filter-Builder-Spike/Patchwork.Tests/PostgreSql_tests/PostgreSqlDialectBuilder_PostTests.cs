using System.Data.Common;
using Dapper;
using Patchwork.SqlDialects.PostgreSql;
using Patchwork.SqlStatements;

namespace Patchwork.Tests.PostgreSql_tests;

public class PostgreSqlDialectBuilder_PostTests
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
    var sut = new PostgreSqlDialectBuilder(ConnectionStringManager.GetPostgreSqlConnectionString());

    // Act
    InsertStatement sql = sut.BuildPostSingleSql("public", "employees", cageJson);
    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("INSERT INTO public.employees", sql.Sql);
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

    using DbConnection connect = sut.GetConnection();
    connect.Open();
    using var transaction = connect.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);

    try
    {
      int changeCount = connect.Execute(sql.Sql, sql.Parameters, transaction);
      dynamic found = connect.QueryFirst("SELECT * FROM employees WHERE lastname = @lastname AND firstname = @firstname", sql.Parameters, transaction);

      Assert.Equal(1, changeCount);
      Assert.Equal("Cage", found.lastname);
      Assert.Equal("x99", found.extension);
    }
    finally
    {
      transaction.Rollback();
      connect.Close();
    }
  }
}
