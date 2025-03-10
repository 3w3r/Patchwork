using Patchwork.DbSchema;

namespace Patchwork.Sort;

public class SortLexer
{
  private readonly string _input;
  private readonly Entity _entity;
  private readonly DatabaseMetadata _metadata;

  public SortLexer(string input, Entity entity, DatabaseMetadata metadata)
  {
    _input = input;
    _entity = entity;
    _metadata = metadata;
  }

  /// <summary>
  /// Tokenizes the input string into a list of SortToken objects.
  /// </summary>
  /// <returns>A list of SortToken objects representing the sorted columns.</returns>
  public List<SortToken> Tokenize()
  {
    // Initialize a list to store the tokens
    List<SortToken> tokens = new List<SortToken>();

    // Split the input string by commas and trim each token
    string[] split = _input.Trim().Split(',');
    foreach (string token in split)
    {
      // Trim the token and check if it's empty
      string tokenValue = token.Trim();
      if (string.IsNullOrEmpty(tokenValue))
        continue;

      // Create a SortToken object for the current token and add it to the list
      SortToken t = MakeValidToken(tokenValue);
      tokens.Add(t);
    }

    // If the entity has a primary key and it's not already included in the tokens,
    // add a SortToken for the primary key with ascending order
    if (_entity.PrimaryKey != null && !tokens.Any(t => t.Column.Equals(_entity.PrimaryKey.Name, StringComparison.OrdinalIgnoreCase)))
      tokens.Add(new SortToken(_entity.Name, _entity.PrimaryKey.Name, SortDirection.Ascending));

    // Return the list of tokens
    return tokens;
  }
  private SortToken MakeValidToken(string columnSort)
  {
    // Check if the first character of the column sort string is a letter
    // If not, throw an ArgumentException with a descriptive error message
    if (!char.IsLetter(columnSort.First()))
      throw new ArgumentException("Column name must begin with a letter.");

    // Check if the column sort string contains a colon
    if (columnSort.Contains(':'))
      // If it does, call the MakeSortedColumnToken method to create a SortToken object with a specific sort direction
      return MakeSortedColumnToken(columnSort);
    else
      // If it doesn't, call the MakeAscendingColumnToken method to create a SortToken object with ascending sort direction
      return MakeAscendingColumnToken(columnSort);
  }

  private SortToken MakeAscendingColumnToken(string columnSort)
  {
    // Validate the column name by checking if it exists in the entity's columns
    // If the column name is not found, throw an ArgumentException with a descriptive error message
    ValidateColumnName(columnSort);

    // Create a new SortToken object with the entity name, column name, and ascending sort direction
    // Return the SortToken object
    return new SortToken(_entity.Name, columnSort, SortDirection.Ascending);
  }

  private void ValidateColumnName(string columnSort)
  {
    // Check if any column in the entity's columns collection has a name that matches the column sort string, ignoring case
    // If no matching column is found, throw an ArgumentException with a descriptive error message
    if (!_entity.Columns.Any(c => c.Name.Equals(columnSort, StringComparison.OrdinalIgnoreCase)))
      throw new ArgumentException($"Column '{columnSort}' does not exist on {_entity.Name}.");
  }

  private SortToken MakeSortedColumnToken(string columnSort)
  {
    // Split the column sort string by the colon character to separate the column name and sort direction
    // If the split result does not contain exactly two elements, throw an ArgumentException with a descriptive error message
    string[] split = columnSort.Split(':');
    if (split.Length != 2)
      throw new ArgumentException("Invalid sort direction. Use only column names and 'asc' or 'desc'.");

    // Trim the first element of the split result (the column name) and validate it
    string name = split[0].Trim();
    ValidateColumnName(name);

    // Initialize the sort direction to ascending
    SortDirection direction = SortDirection.Ascending;

    // Switch on the lowercase version of the second element of the split result (the sort direction)
    switch (split[1].Trim().ToLower())
    {
      case "asc":
        // If the sort direction is "asc", keep the direction as ascending
        break;
      case "desc":
        // If the sort direction is "desc", change the direction to descending
        direction = SortDirection.Descending;
        break;
      default:
        // If the sort direction is neither "asc" nor "desc", throw an ArgumentException with a descriptive error message
        throw new ArgumentException($"Invalid sort order: {split[1].Trim()} is not asc or desc.");
    }

    // Create a new SortToken object with the entity name, column name, and determined sort direction
    // Return the SortToken object
    return new SortToken(_entity.Name, name, direction);
  }
}
