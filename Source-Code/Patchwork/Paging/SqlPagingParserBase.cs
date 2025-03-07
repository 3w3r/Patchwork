namespace Patchwork.Paging;

public abstract class SqlPagingParserBase
{
  protected readonly PagingToken _token;
  public SqlPagingParserBase(PagingToken token)
  {
    _token = token;
  }

  /// <summary>
  /// Parses the SQL paging parameters based on the provided paging token.
  /// </summary>
  /// <returns>A string representing the SQL LIMIT and OFFSET clauses.</returns>
  public virtual string Parse()
  {
    // Construct and return a string representing the SQL LIMIT and OFFSET clauses
    // The LIMIT clause specifies the maximum number of records to return
    // The OFFSET clause specifies the starting point for the records to return
    return $"LIMIT {_token.Limit} OFFSET {_token.Offset}";
  }
}
