namespace Patchwork.Paging;

public class MsSqlPagingParser
{
  private readonly PagingToken _token;
  public MsSqlPagingParser(PagingToken token)
  {
    _token = token;
  }
  public string Parse()
  {
    return $"OFFSET {_token.Offset} ROWS FETCH NEXT {_token.Limit} ROWS ONLY";
  }
}
