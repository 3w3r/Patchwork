using Dapper;
using Patchwork.SqlDialects.MySql;
using Patchwork.SqlStatements;

namespace Patchwork.Tests.MySql_tests;

public class MySqlDialectBuilder_DeleteTests
{
  [Fact, Trait("Category", "LocalOnly")]
  public void BuildDeleteSql_ShouldRemoveResource()
  {
    // Arrange
    var sut = new MySqlDialectBuilder(ConnectionStringManager.GetMySqlConnectionString());

    // Act
    DeleteStatement sql = sut.BuildDeleteSingleSql("classicmodels", "employees", "1625");

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("DELETE FROM classicmodels.employees", sql.Sql);
    Assert.Contains("employeeNumber = @id", sql.Sql);

    using var connect = sut.GetWriterConnection();
    try
    {
      int changeCount = connect.Connection.Execute(sql.Sql, sql.Parameters, connect.Transaction);
      dynamic? found = connect.Connection.QueryFirstOrDefault("SELECT * FROM classicmodels.employees WHERE employeeNumber = @id", sql.Parameters, connect.Transaction);

      Assert.Equal(1, changeCount);
      Assert.Null(found);
    }
    finally
    {
      connect.Transaction.Rollback();
    }
  }
}
