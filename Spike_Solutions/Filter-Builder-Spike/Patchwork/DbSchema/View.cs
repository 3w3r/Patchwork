using System.Collections.ObjectModel;

namespace Patchwork.DbSchema;

public record View(string Name, string Description,
                    string SchemaName, ReadOnlyCollection<Column> Columns)
{
  public Column? PrimaryKey => Columns.FirstOrDefault(c => c.IsPrimaryKey);
}
