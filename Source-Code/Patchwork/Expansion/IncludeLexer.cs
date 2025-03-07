using System.Text;
using Patchwork.DbSchema;

namespace Patchwork.Expansion;

public class IncludeLexer
{
  private readonly string _input;
  private readonly Entity _entity;
  private readonly DatabaseMetadata _meta;
  public IncludeLexer(string input, Entity entity, DatabaseMetadata meta)
  {
    _input = input;
    _entity = entity;
    _meta = meta;
  }

  /// <summary>
  /// Tokenizes the input string representing table relationships and generates a list of <see cref="IncludeToken"/> objects.
  /// </summary>
  /// <returns>A list of <see cref="IncludeToken"/> objects representing the relationships between tables.</returns>
  public List<IncludeToken> Tokenize()
  {
    // Initialize a list to store the tokens
    List<IncludeToken> tokens = new List<IncludeToken>();

    // Start with the parent entity
    Entity parent = _entity;

    // Split the input string by commas and process each segment
    foreach (string segment in _input.Trim().Split(','))
    {
      // Read the identifier from the segment
      string child = ReadIdentifier(segment);

      // Get the child table entity
      Entity childTable = GetChildTableName(child);

      // Check if there is a foreign key from the parent to the child table
      Column? fk = GetEntityForeignKeyToInclude(parent, child, childTable);
      if (fk != null)
      {
        // Get the primary key of the child table
        Column pk = GetPrimaryKeyColumn(childTable);

        // Add a token representing the relationship
        tokens.Add(new IncludeToken(childTable.SchemaName, childTable.Name, pk.Name, parent.SchemaName, parent.Name, fk.Name));
      }
      else
      {
        // Check if there is a foreign key from the child to the parent table
        fk = GetIncludeForeignKeyToEntity(parent, child, childTable);
        if (fk != null)
        {
          // Get the primary key of the parent table
          Column pk = GetPrimaryKeyColumn(parent);

          // Add a token representing the relationship
          tokens.Add(new IncludeToken(childTable.SchemaName, childTable.Name, fk.Name, parent.SchemaName, parent.Name, pk.Name));
        }
        else
        {
          // If no foreign key is found, throw an exception
          throw new ArgumentOutOfRangeException("include", $"The tables {parent.Name} and {childTable.Name} are not related.");
        }
      }

      // Move to the child table as the parent for the next iteration
      parent = childTable;
    }

    // Return the list of tokens
    return tokens;
  }

  private Column GetPrimaryKeyColumn(Entity childTable)
  {
    // Check if the entity has a primary key
    if (childTable.PrimaryKey == null)
    {
      // If the entity does not have a primary key, throw an exception
      throw new InvalidOperationException($"Table {childTable.Name} does not have a primary key.");
    }

    // Return the primary key column of the entity
    return childTable.PrimaryKey;
  }

  // Method to find a foreign key column in the parent table that references the child table
  private Column? GetEntityForeignKeyToInclude(Entity parentTable, string child, Entity childTable)
  {
    // Use LINQ to find the first foreign key column in the parent table that references the child table
    return parentTable.Columns.FirstOrDefault(c => c.IsForeignKey && c.ForeignKeyTableName.Equals(childTable.Name, StringComparison.CurrentCultureIgnoreCase));
  }

  // Method to find a foreign key column in the child table that references the parent table
  private Column? GetIncludeForeignKeyToEntity(Entity parentTable, string child, Entity childTable)
  {
    // Use LINQ to find the first foreign key column in the child table that references the parent table
    return childTable.Columns.FirstOrDefault(c => c.IsForeignKey && c.ForeignKeyTableName.Equals(parentTable.Name, StringComparison.CurrentCultureIgnoreCase));
  }

  private Entity GetChildTableName(string child)
  {
    // Use LINQ to query the schemas and tables to find the child table with the specified name
    Entity? childTable = _meta.Schemas
                          .SelectMany(s => s.Tables)
                          .FirstOrDefault(t => t.Name == child);

    // Check if the child table was found
    if (childTable == null)
    {
      // If the child table was not found, throw an exception
      throw new InvalidOperationException($"Table '{child}' not found.");
    }

    // Return the found child table
    return childTable;
  }

  private string ReadIdentifier(string segment)
  {
    // Initialize the position to 0 and create a StringBuilder to store the identifier
    int position = 0;
    StringBuilder sb = new StringBuilder();

    // Loop through each character in the segment
    while (
      // Continue the loop until the end of the segment is reached
      position < segment.Length
      && (
        // Check if the character is a letter, digit, underscore, or dot
        char.IsLetterOrDigit(segment[position])
        || _input[position] == '_'
        || _input[position] == '.'
      )
    )
    {
      // Append the character to the StringBuilder
      sb.Append(segment[position++]);
    }

    // Convert the StringBuilder to a string and return the identifier
    return sb.ToString();
  }
}
