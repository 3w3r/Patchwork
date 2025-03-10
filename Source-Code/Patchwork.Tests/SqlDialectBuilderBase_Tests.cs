using Patchwork.SqlDialects.PostgreSql;

namespace Patchwork.Tests;

public class SqlDialectBuilderBase_Tests
{
  [Fact]
  public void GetPkValue_ShouldFindValueOfPrimaryKey_WhenObjectIsFromTable()
  {
    // Arrange
    PostgreSqlDialectBuilder dialect = new PostgreSqlDialectBuilder(TestSampleData.DB);
    var sample = new { Id = 42, Quantity = 99, Cost = 47.34 };

    // Act
    var pk = dialect.GetPkValue("dbo", "Orders", sample);

    // Assert
    Assert.NotNull(pk);    
    Assert.Equal(42, int.Parse(pk));
  }
}
