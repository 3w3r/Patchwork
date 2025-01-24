namespace Patchwork.DbSchema;

public record Entity(string Name,
                    string Description,
                    string SchemaName,
                    IList<Column> Columns)
{
  public Column? PrimaryKey => Columns.FirstOrDefault(c => c.IsPrimaryKey);
}
