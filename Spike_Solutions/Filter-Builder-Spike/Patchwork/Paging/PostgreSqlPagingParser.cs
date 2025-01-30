namespace Patchwork.Paging;

public class PostgreSqlPagingParser
{
  private readonly PagingToken _token;
  public PostgreSqlPagingParser(PagingToken token)
  {
    _token = token;
  }
  public string Parse()
  {
    return $"LIMIT {_token.Limit} OFFSET {_token.Offset}";
  }
}
