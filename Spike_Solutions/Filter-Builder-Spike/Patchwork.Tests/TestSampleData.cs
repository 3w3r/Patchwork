using Patchwork.DbSchema;

namespace Patchwork.Tests;

public static class TestSampleData
{
  public static DatabaseMetadata DB = new DatabaseMetadata(
    new List<Schema>()
    {
      new Schema("Shopping", new List<Entity>()
        {
          new Entity("Products", "The Products Table", "Shopping", false,
            new List<Column>()
            {
              new Column("Id", "PK", typeof(long), true, false, "", true, false, true, true),
              new Column("Name", "Name of product", typeof(string), false, false, "", false, false, false, true),
              new Column("Price", "Price of the product", typeof(decimal), false, false, "", false, false, false, true)
            }
          ),
          new Entity("Reviews", "Product Reviews Table", "Shopping", false,
            new List<Column>()
            {
              new Column("Id", "PK", typeof(long), true, false, "", true, false, true, true),
              new Column("ProductId", "FK", typeof(long), false, true, "Products", false, false, false, true),
              new Column("Rating", "Rating of product", typeof(int), false, false, "", false, false, false, false),
              new Column("Review", "Review of product", typeof(string), false, false, "", false, false, false, false)
            }
          )
        },
      new List<Entity>()
    ),
    new Schema("dbo", new List<Entity>()
      {
        new Entity("Orders", "The Orders Table", "dbo", false,
          new List<Column>()
          {
            new Column("Id", "PK", typeof(long), true, false, "", true, false, true, true),
            new Column("Quantity", "Number to purchase", typeof(int), false, false, "", false, false, false, false),
            new Column("Cost", "Line Total", typeof(decimal), false, false, "", false, true, false, false)
          }
        ),
        new Entity("MonkeyTable", "Table with lots of columns for testing", "dbo", false,
          new List<Column>()
          {
              new Column("Id", "PK", typeof(long), true, false, "", true, false, true, true),
              new Column("Name", "Name of widget", typeof(string), false, false, "", false, false, false, true),
              new Column("First_Name", "Name of owner", typeof(string), false, false, "", false, false, false, true),
              new Column("FOO", "Random text", typeof(string), false, false, "", false, false, false, true),
              new Column("Price", "Price of the product", typeof(decimal), false, false, "", false, false, false, true),
              new Column("skillKey", "skill", typeof(string), false, false, "", false, false, false, true),
              new Column("effectiveStartDate", "start", typeof(DateTimeOffset), false, false, "", false, false, false, true),
              new Column("effectiveEndDate", "end", typeof(DateTimeOffset), false, false, "", false, false, false, true),
          }
        ),
        new Entity("SortTest", "Table with lots of columns for sort testing", "dbo", false,
          new List<Column>()
          {
              new Column("A", "PK", typeof(long), true, false, "", true, false, true, true),
              new Column("FirstName", "Name of widget", typeof(string), false, false, "", false, false, false, true),
              new Column("LastName", "Name of widget", typeof(string), false, false, "", false, false, false, true),
              new Column("But_I_Think_This_Column_Name_Is_Really_Long", "Name of widget", typeof(string), false, false, "", false, false, false, true),
              new Column("B", "Name of widget", typeof(string), false, false, "", false, false, false, true),
              new Column("C", "Name of owner", typeof(string), false, false, "", false, false, false, true),
              new Column("D", "Random text", typeof(string), false, false, "", false, false, false, true),
              new Column("E", "Price of the product", typeof(decimal), false, false, "", false, false, false, true),
              new Column("F", "skill", typeof(string), false, false, "", false, false, false, true),
              new Column("effectiveStartDate", "start", typeof(DateTimeOffset), false, false, "", false, false, false, true),
              new Column("effectiveEndDate", "end", typeof(DateTimeOffset), false, false, "", false, false, false, true),
          }
        ),
      },
      new List<Entity>()
    )}
  );

}
