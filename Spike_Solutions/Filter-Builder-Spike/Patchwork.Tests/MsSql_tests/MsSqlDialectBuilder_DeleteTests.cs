using System.Data.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
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
    DeleteStatement sql = sut.BuildDeleteSingleSql("dbo", "employees", "1216");

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("DELETE FROM [classicmodels].[employees]", sql.Sql);
    Assert.Contains("[employeeNumber] = @id", sql.Sql);

    using DbConnection connect = sut.GetConnection();
    connect.Open();

    using var transaction = connect.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);
    try
    {
      int changeCount = connect.Execute(sql.Sql, sql.Parameters, transaction);
      dynamic? found = connect.QueryFirstOrDefault("SELECT * FROM [classicmodels].[employees] WHERE [employeeNumber] = @id", sql.Parameters, transaction);

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
