﻿using Dapper;

using Patchwork.SqlDialects;
using Patchwork.SqlDialects.MsSql;
using Patchwork.SqlStatements;

namespace Patchwork.Tests.MsSql_tests;

public class MsSqlDialectBuilder_DeleteTests
{
  [SkippableFact, Trait("Category", "LocalOnly")]
  public void BuildDeleteSql_ShouldRemoveResource()
  {
    // Arrange
    var connectionstring = string.Empty;
    try { connectionstring = ConnectionStringManager.GetMsSqlConnectionString(); } catch { }
    Skip.If(string.IsNullOrEmpty(connectionstring));

    ISqlDialectBuilder sut = new MsSqlDialectBuilder(connectionstring);
    try { sut.DiscoverSchema(); } catch { Skip.If(true, "Database schema discovery failed"); }

    // Act
    DeleteStatement sql = sut.BuildDeleteSingleSql("classicmodels", "employees", "1216");

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("DELETE FROM [classicmodels].[employees]", sql.Sql);
    Assert.Contains("[employeeNumber] = @id", sql.Sql);

    using var connect = sut.GetWriterConnection();
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
