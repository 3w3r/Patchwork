using System.Collections.ObjectModel;

namespace Patchwork.Schema;

public record Table(string Name, string Description, ReadOnlyCollection<Column> Columns)
{
  public Column? PrimaryKey => Columns.FirstOrDefault(c => c.IsPrimaryKey);
}
