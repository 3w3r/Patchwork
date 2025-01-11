using MySqlConnector;

//Connection string includes username and password... will need to be connected to security
Console.WriteLine("Enter Username:");
var user = Console.ReadLine();
Console.WriteLine("Enter Password:");
var pass = Console.ReadLine();
using (var connection = new MySqlConnection($"Server=mysql.markewer.com;User ID={user};Password={pass};Database=taskboard;"))
{
    var dbReader = new DatabaseSchemaReader.DatabaseReader(connection);

    //AllTables returns schema without users or data
    var all = dbReader.AllTables();

    foreach (var table in all)
    {
        Console.WriteLine(table.SchemaOwner + "/" + table.Name);
        foreach (var column in table.Columns)
        {
            Console.Write("    " + column.Name);
            if (column.IsPrimaryKey)
                Console.Write(" [PK]");
            if (column.IsForeignKey)
                Console.Write($" [FK: {column.ForeignKeyTableName}]");
            Console.Write("\n");
        }
        Console.Write("\n");
    }
    Console.WriteLine("Press any key to close...");
    Console.ReadKey();
}