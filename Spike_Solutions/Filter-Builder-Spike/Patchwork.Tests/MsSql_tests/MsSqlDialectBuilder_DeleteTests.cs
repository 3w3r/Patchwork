using Dapper;
using Patchwork.Api;
using Patchwork.SqlDialects.MsSql;
using Patchwork.SqlStatements;

namespace Patchwork.Tests.MsSql_tests;

public class MsSqlDialectBuilder_DeleteTests
{
  [Fact]
  public void BuildDeleteSql_ShouldRemoveResource()
  {
    // Arrange
    MsSqlDialectBuilder sut = new MsSqlDialectBuilder(ConnectionStringManager.GetMsSqlConnectionString());

    // Act
    DeleteStatement sql = sut.BuildDeleteSingleSql("classicmodels", "employees", "1216");

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("DELETE FROM [classicmodels].[employees]", sql.Sql);
    Assert.Contains("[employeeNumber] = @id", sql.Sql);

    using var connect = sut.GetConnection();
    try
    {
      int changeCount = connect.Connection.Execute(sql.Sql, sql.Parameters, connect.Transaction);
      dynamic? found = connect.Connection.QueryFirstOrDefault("SELECT * FROM [classicmodels].[employees] WHERE [employeeNumber] = @id",
                                                              sql.Parameters, connect.Transaction);

      Assert.Equal(1, changeCount);
      Assert.Null(found);
    }
    finally
    {
      connect.Transaction.Rollback();
    }
  }
}
