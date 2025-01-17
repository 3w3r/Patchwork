namespace Patchwork.Sort;

public class SortToken
{
  public SortToken(string column, SortDirection direction)
  {
    Column = column;
    Direction = direction;
  }
  public string Column { get; set; } = string.Empty;
  public SortDirection Direction { get; set; } = SortDirection.Ascending;
}
