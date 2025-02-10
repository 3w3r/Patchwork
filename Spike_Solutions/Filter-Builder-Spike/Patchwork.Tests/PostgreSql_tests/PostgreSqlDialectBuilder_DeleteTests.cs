using System.Data.Common;
using Dapper;
using Npgsql;
using Patchwork.SqlDialects.PostgreSql;
using Patchwork.SqlStatements;

namespace Patchwork.Tests.PostgreSql_tests;

public class PostgreSqlDialectBuilder_DeleteTests
{
  [Fact]
  public void BuildDeleteSql_ShouldRemoveResource()
  {
    // Arrange
    var sut = new PostgreSqlDialectBuilder(ConnectionStringManager.GetPostgreSqlConnectionString());

    // Act
    DeleteStatement sql = sut.BuildDeleteSingleSql("public", "employees", "1625");

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("DELETE FROM public.employees", sql.Sql);
    Assert.Contains("employeenumber = @id", sql.Sql);

    using DbConnection connect = sut.GetConnection();
    connect.Open();
    using var transaction = connect.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);
    try
    {
      int changeCount = connect.Execute(sql.Sql, sql.Parameters, transaction);
      dynamic? found = connect.QueryFirstOrDefault("SELECT * FROM public.employees WHERE employeeNumber = @id", sql.Parameters, transaction);

      Assert.Equal(1, changeCount);
      Assert.Null(found);
    }
    finally
    {
      transaction.Rollback();
      connect.Close();
    }
  }
}
