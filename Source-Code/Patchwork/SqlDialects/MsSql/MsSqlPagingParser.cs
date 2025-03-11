using Patchwork.Paging;

namespace Patchwork.SqlDialects.MsSql;

public class MsSqlPagingParser : SqlPagingParserBase
{
  public MsSqlPagingParser(PagingToken token) : base(token) { }

  /// <summary>
  /// Parses the paging token and generates a SQL statement for pagination.
  /// </summary>
  /// <returns>The generated SQL statement as a string.</returns>
  public override string Parse()
  {
    // The OFFSET clause specifies the number of rows to skip before starting to return rows.
    // The FETCH NEXT clause specifies the maximum number of rows to return.
    // The ROWS ONLY clause ensures that only rows are returned and no additional columns are included.
    return $"OFFSET {_token.Offset} ROWS FETCH NEXT {_token.Limit} ROWS ONLY";
  }
}
