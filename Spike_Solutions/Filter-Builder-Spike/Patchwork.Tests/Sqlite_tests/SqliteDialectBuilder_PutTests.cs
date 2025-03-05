using System.Text.Json;
using Dapper;
using Json.Patch;
using Patchwork.Authorization;
using Patchwork.Repository;
using Patchwork.SqlDialects.Sqlite;
using Patchwork.SqlStatements;

namespace Patchwork.Tests.Sqlite_tests;

public class SqliteDialectBuilder_PatchworkLogTests
{
  private readonly string pacthJson = "[" +
    "{\"op\":\"replace\",\"path\":\"/productname\",\"value\":\"2024 Factory 5 MK2 Roadster\"}," +
    "{\"op\":\"replace\",\"path\":\"/productscale\",\"value\":\"1:10\"}," +
    "{\"op\":\"replace\",\"path\":\"/productvendor\",\"value\":\"Motor City Art Classics\"}" +
    "]";

  [Fact]
  public void BuildPatchworkLogSql_ShouldBuildInsertStatement_ForPatchworkLog()
  {
    // Arrange
    DefaultPatchworkAuthorization auth = new DefaultPatchworkAuthorization();
    SqliteDialectBuilder sql = new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString());
    PatchworkRepository sut = new PatchworkRepository(auth, sql);
    JsonPatch? patch = JsonSerializer.Deserialize<JsonPatch>(pacthJson);
    if (patch == null)
      Assert.Fail();
    using var connect = sql.GetWriterConnection();

    // Act
    InsertStatement insert = sql.GetInsertStatementForPatchworkLog("classicmodels", "products", "ME_9999", patch);
    var success = sut.AddPatchToLog(connect, "classicmodels", "products", "ME_9999", patch);

    // Assert
    Assert.NotNull(insert);
    Assert.Equal(4, insert.Parameters.Count);
    Assert.Equal("classicmodels", insert.Parameters["schemaname"].ToString());
    Assert.Equal("products", insert.Parameters["entityname"].ToString());
    Assert.Equal("ME_9999", insert.Parameters["id"].ToString());

    var found = connect.Connection.Query("Select * from patchwork_event_log", connect.Transaction);
    Assert.NotNull(found);
    Assert.Equal(1, found?.Count());

    connect.Transaction.Rollback();
  }
}
public class SqliteDialectBuilder_PutTests
{
  private readonly JsonDocument katoJsonOriginal = JsonDocument.Parse("{ \n" +
                                                                      "  \"employeeNumber\": \"1625\", \n" +
                                                                      "  \"lastName\": \"Kato\", \n" +
                                                                      "  \"firstName\": \"Yoshimi\", \n" +
                                                                      "  \"extension\": \"x102\", \n" +
                                                                      "  \"email\": \"ykato@classicmodelcars.com\", \n" +
                                                                      "  \"officeCode\": \"5\", \n" +
                                                                      "  \"reportsTo\": \"1621\", \n" +
                                                                      "  \"jobTitle\": \"Sales Rep\" \n" +
                                                                      "}");

  private readonly JsonDocument katoJsonUpdate = JsonDocument.Parse("{ \n" +
                                                                    "  \"employeeNumber\": \"1625\", \n" +
                                                                    "  \"lastName\": \"Kato\", \n" +
                                                                    "  \"firstName\": \"Yoshimi\", \n" +
                                                                    "  \"extension\": \"x104\", \n" +
                                                                    "  \"email\": \"ykato@classicmodelcars.com\", \n" +
                                                                    "  \"officeCode\": \"5\", \n" +
                                                                    "  \"reportsTo\": \"1621\", \n" +
                                                                    "  \"jobTitle\": \"Sales Rep\" \n" +
                                                                    "}");

  [Fact]
  public void BuildPutSql_ShouldUpdateResource_WhenJsonIsChanged()
  {
    // Arrange
    SqliteDialectBuilder sut = new SqliteDialectBuilder(ConnectionStringManager.GetSqliteConnectionString());

    // Act
    UpdateStatement sql = sut.BuildPutSingleSql("dbo", "employees", "1625", katoJsonUpdate);

    // Assert
    Assert.NotEmpty(sql.Sql);
    Assert.Contains("UPDATE employees", sql.Sql);
    Assert.Contains("SET", sql.Sql);
    Assert.DoesNotContain("employeeNumber = @employeeNumber", sql.Sql);
    Assert.Contains("lastName = @lastName", sql.Sql);
    Assert.Contains("firstName = @firstName", sql.Sql);
    Assert.Contains("email = @email", sql.Sql);
    Assert.Contains("extension = @extension", sql.Sql);

    using SqlDialects.WriterConnection connect = sut.GetWriterConnection();

    try
    {
      int changeCount = connect.Connection.Execute(sql.Sql, sql.Parameters, connect.Transaction);
      dynamic found = connect.Connection.QueryFirst("SELECT * FROM employees WHERE employeeNumber = @id", sql.Parameters, connect.Transaction);

      Assert.Equal(1, changeCount);
      Assert.Equal("Kato", found.lastName);
      Assert.Equal("x104", found.extension);
    }
    finally
    {
      connect.Transaction.Rollback();
    }
  }
}
