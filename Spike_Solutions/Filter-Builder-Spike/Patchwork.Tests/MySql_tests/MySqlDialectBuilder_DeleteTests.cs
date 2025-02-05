using Dapper;
using MySqlConnector;
using Patchwork.SqlDialects.MySql;
using Patchwork.SqlStatements;

namespace Patchwork.Tests.MySql_tests;

public class MySqlDialectBuilder_DeleteTests
{
  [Fact]
  public void BuildDeleteSql_ShouldRemoveResource()
  {
    // Arrange
    var sut = new MySqlDialectBuilder(ConnectionStringManager.GetMySqlConnectionString());

    // Act
    DeleteStatement sql = sut.BuildDeleteSingleSql("taskboard", "employees", "1625");

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("DELETE FROM `taskboard`.`employees`", sql.Sql);
    Assert.Contains("`employeeNumber` = @id", sql.Sql);

    using var connect = new MySqlConnection(ConnectionStringManager.GetMySqlConnectionString());
    connect.Open();
    using var transaction = connect.BeginTransaction();
    try
    {
      int changeCount = connect.Execute(sql.Sql, sql.Parameters, transaction);
      dynamic? found = connect.QueryFirstOrDefault("SELECT * FROM `taskboard`.`employees` WHERE `employeeNumber` = @id", sql.Parameters, transaction);

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
