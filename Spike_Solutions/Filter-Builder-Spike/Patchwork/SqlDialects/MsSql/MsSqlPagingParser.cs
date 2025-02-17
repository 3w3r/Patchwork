using Patchwork.Paging;

namespace Patchwork.SqlDialects.MsSql;

public class MsSqlPagingParser : SqlPagingParserBase
{
  public MsSqlPagingParser(PagingToken token) : base(token) { }

  public override string Parse()
  {
    return $"OFFSET {_token.Offset} ROWS FETCH NEXT {_token.Limit} ROWS ONLY";
  }
}
