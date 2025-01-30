using Patchwork.DbSchema;

namespace Patchwork.Sort;

public class SortLexer
{
  private string _input;
  private readonly Entity _entity;
  private readonly DatabaseMetadata _metadata;

  public SortLexer(string input, Entity entity, DatabaseMetadata metadata)
  {
    _input = input;
    _entity = entity;
    _metadata = metadata;
  }

  public List<SortToken> Tokenize()
  {
    List<SortToken> tokens = new List<SortToken>();
    string[] split = _input.Trim().Split(',');
    foreach (string token in split)
    {
      string tokenValue = token.Trim();
      SortToken t = MakeValidToken(tokenValue);
      tokens.Add(t);
    }

    if (_entity.PrimaryKey != null && !tokens.Any(t => t.Column.Equals(_entity.PrimaryKey.Name, StringComparison.OrdinalIgnoreCase)))
      tokens.Add(new SortToken(_entity.Name, _entity.PrimaryKey.Name, SortDirection.Ascending));

    return tokens;
  }
  public SortToken MakeValidToken(string columnSort)
  {
    if (!char.IsLetter(columnSort.First()))
      throw new ArgumentException("Column name must begin with a letter.");

    if (columnSort.Contains(':'))
      return MakeSortedColumnToken(columnSort);
    else
      return MakeAscendingColumnToken(columnSort);
  }

  private SortToken MakeAscendingColumnToken(string columnSort)
  {
    ValidateColumnName(columnSort);
    return new SortToken(_entity.Name, columnSort, SortDirection.Ascending);
  }

  private void ValidateColumnName(string columnSort)
  {
    if (!_entity.Columns.Any(c => c.Name.Equals(columnSort, StringComparison.OrdinalIgnoreCase)))
      throw new ArgumentException($"Column '{columnSort}' does not exist on {_entity.Name}.");
  }

  private SortToken MakeSortedColumnToken(string columnSort)
  {
    string[] split = columnSort.Split(':');
    if (split.Length != 2)
      throw new ArgumentException("Invalid sort direction. Use only column names and 'asc' or 'desc'.");
    string name = split[0].Trim();
    ValidateColumnName(name);

    SortDirection direction = SortDirection.Ascending;
    switch (split[1].Trim().ToLower())
    {
      case "asc":
        break;
      case "desc":
        direction = SortDirection.Descending;
        break;
      default:
        throw new ArgumentException($"Invalid sort order: {split[1].Trim()} is not asc or desc.");
    }

    return new SortToken(_entity.Name, name, direction);
  }
}
