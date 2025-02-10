using System.Data.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Patchwork.SqlDialects;
using Patchwork.SqlDialects.MsSql;
using Patchwork.SqlStatements;

namespace Patchwork.Tests.MsSql_tests;

public class MsSqlDialectBuilder_PutTests
{
  private readonly string katoJsonOriginal = "{ \n" +
    "  \"employeeNumber\": \"1625\", \n" +
    "  \"lastName\": \"Kato\", \n" +
    "  \"firstName\": \"Yoshimi\", \n" +
    "  \"extension\": \"x102\", \n" +
    "  \"email\": \"ykato@classicmodelcars.com\", \n" +
    "  \"officeCode\": \"5\", \n" +
    "  \"reportsTo\": \"1621\", \n" +
    "  \"jobTitle\": \"Sales Rep\" \n" +
    "}";

  private readonly string katoJsonUpdate = "{ \n" +
  "  \"employeeNumber\": \"1625\", \n" +
  "  \"lastName\": \"Kato\", \n" +
  "  \"firstName\": \"Yoshimi\", \n" +
  "  \"extension\": \"x104\", \n" +
  "  \"email\": \"ykato@classicmodelcars.com\", \n" +
  "  \"officeCode\": \"5\", \n" +
  "  \"reportsTo\": \"1621\", \n" +
  "  \"jobTitle\": \"Sales Rep\" \n" +
  "}";

  [Fact]
  public void BuildPutSql_ShouldUpdateResource_WhenJsonIsChanged()
  {
    // Arrange
    ISqlDialectBuilder sut = new MsSqlDialectBuilder(ConnectionStringManager.GetMsSqlConnectionString());

    // Act
    UpdateStatement sql = sut.BuildPutSingleSql("classicmodels", "employees", "1625", katoJsonUpdate);

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("UPDATE [classicmodels].[employees]", sql.Sql);
    Assert.Contains("SET", sql.Sql);
    Assert.DoesNotContain("[employeeNumber] = @employeeNumber", sql.Sql);
    Assert.Contains("[lastName] = @lastName", sql.Sql);
    Assert.Contains("[firstName] = @firstName", sql.Sql);
    Assert.Contains("[email] = @email", sql.Sql);
    Assert.Contains("[extension] = @extension", sql.Sql);

    using DbConnection connect = sut.GetConnection();
    connect.Open();
    using var transaction = connect.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);
    try
    {
      int changeCount = connect.Execute(sql.Sql, sql.Parameters, transaction);
      dynamic found = connect.QueryFirst("SELECT * FROM [classicmodels].[employees] WHERE [employeeNumber] = @id", sql.Parameters, transaction);

      Assert.Equal(1, changeCount);
      Assert.Equal("Kato", found.lastName);
      Assert.Equal("x104", found.extension);
    }
    finally
    {
      transaction.Rollback();
      connect.Close();
    }
  }
}
