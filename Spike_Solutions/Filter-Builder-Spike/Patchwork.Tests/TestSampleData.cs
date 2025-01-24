using Patchwork.DbSchema;

namespace Patchwork.Tests;

public static class TestSampleData
{
  public static DatabaseMetadata DB = new DatabaseMetadata(
    new List<Schema>()
    {
      new Schema("Shopping", new List<Entity>()
        {
          new Entity("Products", "The Products Table", "Shopping",
            new List<Column>()
            {
              new Column("Id", "PK", "bigint", true, false, "", true, false, true, true),
              new Column("Name", "Name of product", "text", false, false, "", false, false, false, true),
              new Column("Price", "Price of the product", "decimal(10, 2)", false, false, "", false, false, false, true)
            }
          ),
          new Entity("Reviews", "Product Reviews Table", "Shopping",
            new List<Column>()
            {
              new Column("Id", "PK", "bigint", true, false, "", true, false, true, true),
              new Column("ProductId", "FK", "bigint", false, true, "Products", false, false, false, true),
              new Column("Rating", "Rating of product", "int", false, false, "", false, false, false, false),
              new Column("Review", "Review of product", "text", false, false, "", false, false, false, false)
            }
          )
        },
      new List<Entity>()
    ),
    new Schema("dbo", new List<Entity>()
      {
        new Entity("Orders", "The Orders Table", "dbo",
          new List<Column>()
          {
            new Column("Id", "PK", "bigint", true, false, "", true, false, true, true),
            new Column("Quantity", "Number to purchase", "int", false, false, "", false, false, false, false),
            new Column("Cost", "Line Total", "decimal(10, 2)", false, false, "", false, true, false, false)
          }
        ),
        new Entity("MonkeyTable", "Table with lots of columns for testing", "dbo",
          new List<Column>()
          {
              new Column("Id", "PK", "bigint", true, false, "", true, false, true, true),
              new Column("Name", "Name of widget", "text", false, false, "", false, false, false, true),
              new Column("First_Name", "Name of owner", "text", false, false, "", false, false, false, true),
              new Column("FOO", "Random text", "text", false, false, "", false, false, false, true),
              new Column("Price", "Price of the product", "decimal(10, 2)", false, false, "", false, false, false, true),
              new Column("skillKey", "skill", "text", false, false, "", false, false, false, true),
              new Column("effectiveStartDate", "start", "datetime", false, false, "", false, false, false, true),
              new Column("effectiveEndDate", "end", "datetime", false, false, "", false, false, false, true),
          }
        ),
        new Entity("SortTest", "Table with lots of columns for sort testing", "dbo",
          new List<Column>()
          {
              new Column("A", "PK", "bigint", true, false, "", true, false, true, true),
              new Column("FirstName", "Name of widget", "text", false, false, "", false, false, false, true),
              new Column("LastName", "Name of widget", "text", false, false, "", false, false, false, true),
              new Column("But_I_Think_This_Column_Name_Is_Really_Long", "Name of widget", "text", false, false, "", false, false, false, true),
              new Column("B", "Name of widget", "text", false, false, "", false, false, false, true),
              new Column("C", "Name of owner", "text", false, false, "", false, false, false, true),
              new Column("D", "Random text", "text", false, false, "", false, false, false, true),
              new Column("E", "Price of the product", "decimal(10, 2)", false, false, "", false, false, false, true),
              new Column("F", "skill", "text", false, false, "", false, false, false, true),
              new Column("effectiveStartDate", "start", "datetime", false, false, "", false, false, false, true),
              new Column("effectiveEndDate", "end", "datetime", false, false, "", false, false, false, true),
          }
        ),
      },
      new List<Entity>()
    )}
  );

}
