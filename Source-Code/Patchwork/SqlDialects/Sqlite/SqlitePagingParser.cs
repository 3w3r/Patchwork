using Patchwork.Paging;

namespace Patchwork.SqlDialects.Sqlite;

public class SqlitePagingParser : SqlPagingParserBase
{
  public SqlitePagingParser(PagingToken token) : base(token) { }
}