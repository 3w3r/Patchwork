namespace Patchwork.Paging;

public abstract class SqlPagingParserBase
{
  protected readonly PagingToken _token;
  public SqlPagingParserBase(PagingToken token)
  {
    _token = token;
  }

  public virtual string Parse()
  {
    return $"LIMIT {_token.Limit} OFFSET {_token.Offset}";
  }
}
