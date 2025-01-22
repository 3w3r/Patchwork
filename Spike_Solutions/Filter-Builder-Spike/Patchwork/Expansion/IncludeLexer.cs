using System.Text;
using Patchwork.DbSchema;

namespace Patchwork.Expansion;

public class IncludeLexer
{
  private readonly string _input;
  private readonly Table _entity;
  private readonly DatabaseMetadata _meta;
  public IncludeLexer(string input, Table entity, DatabaseMetadata meta)
  {
    _input = input;
    _entity = entity;
    _meta = meta;
  }

  public List<IncludeToken> Tokenize()
  {
    var tokens = new List<IncludeToken>();
    foreach (var segment in _input.Trim().Split(','))
    {
      var child = ReadIdentifier(segment);
      var childTable = GetChildTableName(child);
      var pk = GetPrimaryKeyColumn(childTable);
      var fk = GetForeignKeyColumn(child, childTable);

      tokens.Add(new IncludeToken(childTable.Name, pk.Name, _entity.Name, fk.Name));
    }

    return tokens;
  }

  private Column GetPrimaryKeyColumn(Table childTable)
  {
    if (childTable.PrimaryKey == null) throw new InvalidOperationException($"Table {childTable.Name} does not have a primary key.");
    return childTable.PrimaryKey;
  }

  private Column GetForeignKeyColumn(string child, Table childTable)
  {
    var fk = _entity.Columns.FirstOrDefault(c => c.IsForeignKey && c.ForeignKeyTableName.Equals(childTable.Name, StringComparison.CurrentCultureIgnoreCase));
    if (fk == null) throw new InvalidOperationException($"Table {_entity.Name} does not have a foreign key to {child}.");
    return fk;
  }

  private Table GetChildTableName(string child)
  {
    var childTable = _meta.Schemas
                          .SelectMany(s => s.Tables)
                          .FirstOrDefault(t => t.Name == child);

    if (childTable == null) throw new InvalidOperationException($"Table '{child}' not found.");
    return childTable;
  }

  private string ReadIdentifier(string segment)
  {
    var position = 0;
    var sb = new StringBuilder();
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
    var value = sb.ToString();
    return value;
  }
}
