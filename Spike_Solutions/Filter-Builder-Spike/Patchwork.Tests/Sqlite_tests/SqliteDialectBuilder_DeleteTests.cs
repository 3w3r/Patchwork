using Dapper;
using Microsoft.Data.Sqlite;
using Patchwork.SqlDialects.Sqlite;
using Patchwork.SqlStatements;

namespace Patchwork.Tests.Sqlite_tests;

public class SqliteDialectBuilder_DeleteTests
{
  [Fact]
  public void BuildDeleteSql_ShouldRemoveResource()
  {
    // Arrange
    SqliteDialectBuilder sut = new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString());

    // Act
    DeleteStatement sql = sut.BuildDeleteSingleSql("dbo", "employees", "1625");

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("DELETE FROM employees", sql.Sql);
    Assert.Contains("employeeNumber = @id", sql.Sql);

    using SqliteConnection connect = new SqliteConnection(ConnectionStringManager.GetSqliteConnectionString());
    connect.Open();
    using SqliteTransaction transaction = connect.BeginTransaction();
    try
    {
      int changeCount = connect.Execute(sql.Sql, sql.Parameters, transaction);
      dynamic? found = connect.QueryFirstOrDefault("SELECT * FROM employees WHERE employeeNumber = @id", sql.Parameters, transaction);

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
