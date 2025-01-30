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

  public List<IncludeToken> Tokenize()
  {
    List<IncludeToken> tokens = new List<IncludeToken>();
    foreach (string segment in _input.Trim().Split(','))
    {
      string child = ReadIdentifier(segment);
      Entity childTable = GetChildTableName(child);
      Column fk = GetEntityForeignKeyToInclude(child, childTable);
      if(fk!=null)
      {
        Column pk = GetPrimaryKeyColumn(childTable);
        tokens.Add(new IncludeToken(childTable.SchemaName, childTable.Name, pk.Name, _entity.SchemaName, _entity.Name, fk.Name));
      }
      else
      {
        fk=GetIncludeForeignKeyToEntity(child, childTable);
        if (fk != null)
        {
          Column pk = GetPrimaryKeyColumn(_entity);
          tokens.Add(new IncludeToken(childTable.SchemaName, childTable.Name, pk.Name, _entity.SchemaName, _entity.Name, fk.Name));
        }
        else
        {
          throw new ArgumentOutOfRangeException("include", $"The tables {_entity.Name} and {childTable.Name} are not related.");
        }
      }
    }

    return tokens;
  }

  private Column GetPrimaryKeyColumn(Entity childTable)
  {
    if (childTable.PrimaryKey == null)
      throw new InvalidOperationException($"Table {childTable.Name} does not have a primary key.");
    return childTable.PrimaryKey;
  }

  private Column GetEntityForeignKeyToInclude(string child, Entity childTable)
  {
    Column? fk = _entity.Columns.FirstOrDefault(c => c.IsForeignKey && c.ForeignKeyTableName.Equals(childTable.Name, StringComparison.CurrentCultureIgnoreCase));
    //if (fk == null)
    //  throw new InvalidOperationException($"Table {_entity.Name} does not have a foreign key to {child}.");
    return fk;
  }
  private Column GetIncludeForeignKeyToEntity(string child, Entity childTable)
  {
    Column? fk = childTable.Columns.FirstOrDefault(c => c.IsForeignKey && c.ForeignKeyTableName.Equals(_entity.Name, StringComparison.CurrentCultureIgnoreCase));
    //if (fk == null)
    //  throw new InvalidOperationException($"Table {_entity.Name} does not have a foreign key to {child}.");
    return fk;
  }

  private Entity GetChildTableName(string child)
  {
    Entity? childTable = _meta.Schemas
                          .SelectMany(s => s.Tables)
                          .FirstOrDefault(t => t.Name == child);

    if (childTable == null)
      throw new InvalidOperationException($"Table '{child}' not found.");
    return childTable;
  }

  private string ReadIdentifier(string segment)
  {
    int position = 0;
    StringBuilder sb = new StringBuilder();
    while (
      position < segment.Length
      && (char.IsLetterOrDigit(segment[position])
         || _input[position] == '_'
         || _input[position] == '.'
         )
      )
    {
      sb.Append(segment[position++]);
    }
    string value = sb.ToString();
    return value;
  }
}
