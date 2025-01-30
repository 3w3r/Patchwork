using MySqlConnector;

//Connection string includes username and password... will need to be connected to security
Console.WriteLine("Enter Username:");
var user = Console.ReadLine();
Console.WriteLine("Enter Password:");
var pass = Console.ReadLine();
using (var connection = new MySqlConnection($"Server=mysql.markewer.com;User ID={user};Password={pass};Database=taskboard;"))
{
    var dbReader = new DatabaseSchemaReader.DatabaseReader(connection);

    var schemas = dbReader.AllSchemas();
    Console.WriteLine("====[ Database Schemas ]==============");
    foreach (var schema in schemas)
    {
        Console.WriteLine($"SCHEMA: {schema.Name}");
    }
    Console.WriteLine("--------------------------------------");
    Console.WriteLine();

    var tables = dbReader.AllTables();
    Console.WriteLine("====[ Database Tables ]===============");
    foreach (var table in tables)
    {
        Console.WriteLine($"TABLE: {table.SchemaOwner}/{table.Name}");
        foreach (var column in table.Columns)
        {
            Console.Write("    " + column.Name);
            if (column.IsPrimaryKey)
                Console.Write(" [PK]");
            if (column.IsForeignKey)
                Console.Write($" [FK: {column.ForeignKeyTableName}]");
            Console.Write("\n");
        }
        Console.WriteLine();
    }
    Console.WriteLine("--------------------------------------");
    Console.WriteLine();

    var views = dbReader.AllViews();
    Console.WriteLine("====[ Database Views ]================");
    foreach (var view in views)
    {
        Console.WriteLine($"VIEW: {view.SchemaOwner}/{view.Name}");
        foreach (var column in view.Columns)
        {
            Console.Write("    " + column.Name);
            if (column.IsPrimaryKey)
                Console.Write(" [PK]");
            if (column.IsForeignKey)
                Console.Write($" [FK: {column.ForeignKeyTableName}]");
            Console.Write("\n");
        }
        Console.WriteLine();
    }
    Console.WriteLine("--------------------------------------");
    Console.WriteLine();

    Console.WriteLine("Press any key to close...");
    Console.ReadKey();
}
