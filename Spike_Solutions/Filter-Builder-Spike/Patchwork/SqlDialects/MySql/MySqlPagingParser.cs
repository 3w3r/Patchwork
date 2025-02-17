using Patchwork.Paging;

namespace Patchwork.SqlDialects.MySql;

public class MySqlPagingParser : SqlPagingParserBase
{
  public MySqlPagingParser(PagingToken token) : base(token) { }
}
