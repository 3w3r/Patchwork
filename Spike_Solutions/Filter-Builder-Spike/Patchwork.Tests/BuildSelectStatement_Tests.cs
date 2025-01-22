using Patchwork.DbSchema;
using Patchwork.SqlDialects;

namespace Patchwork.Tests;

public class BuildSelectStatement_Tests
{
  public static DatabaseMetadata DB = new DatabaseMetadata(
    new List<Schema>(){
    new Schema("Shopping", new List<Table>(){
      new Table("Products", "The Products Table", "dbo",
        new List<Column>(){
          new Column("Id", "PK", "bigint", true, false, "", true, false, true, true),
          new Column("Name", "Name of product", "text", false, false, "", false, false, false, true),
          new Column("Price", "Price of the product", "decimal(10, 2)", false, false, "", false, false, false, true)
      }.AsReadOnly()),
    }.AsReadOnly(),
    new List<View>().AsReadOnly()),
    new Schema("dbo", new List<Table>(){
      new Table("Orders", "The Orders Table", "dbo",
        new List<Column>(){
          new Column("Id", "PK", "bigint", true, false, "", true, false, true, true),
          new Column("Quantity", "Number to purchase", "int", false, false, "", false, false, false, false),
          new Column("Cost", "Line Total", "decimal(10, 2)", false, false, "", false, true, false, false)
      }.AsReadOnly()),
    }.AsReadOnly(),
    new List<View>().AsReadOnly())
    }.AsReadOnly()
    );

  [Fact]
  public void BuildSelectStatement_MsSql()
  {
    // Arrange
    var dialect = new MsSqlDialectBuilder("");
    var columns = new List<Column>().AsReadOnly();
    var t = new Table("Products", "The Products Table", "Shopping", columns);

    // Act
    var select = dialect.BuildSelectClause(t);

    // Assert
    Assert.Equal("SELECT * FROM [Shopping].[Products] AS [T_Products]", select);
  }
  [Fact]
  public void BuildSelectStatement_MySql()
  {
    // Arrange
    var dialect = new MySqlDialectBuilder("");
    var columns = new List<Column>().AsReadOnly();
    var t = new Table("Products", "The Products Table", "Shopping", columns);

    // Act
    var select = dialect.BuildSelectClause(t);

    // Assert
    Assert.Equal("SELECT * FROM shopping.products AS t_products", select);
  }
  [Fact]
  public void BuildSelectStatement_PostgreSql()
  {
    // Arrange
    var dialect = new PostgreSqlDialectBuilder("");
    var columns = new List<Column>().AsReadOnly();
    var t = new Table("Products", "The Products Table", "Shopping", columns);

    // Act
    var select = dialect.BuildSelectClause(t);

    // Assert
    Assert.Equal("SELECT * FROM shopping.products AS t_products", select);
  }
}
