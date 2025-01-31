using Patchwork.Paging;

namespace Patchwork.SqlDialects.PostgreSql;
public class PostgreSqlPagingParser : SqlPagingParserBase
{
  public PostgreSqlPagingParser(PagingToken token) : base(token) { }
}
