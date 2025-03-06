using Dapper;
using Patchwork.Paging;
using Patchwork.SqlDialects;
using Patchwork.SqlStatements;
using static Dapper.SqlMapper;

namespace Patchwork.Repository;


public partial class PatchworkRepository : IPatchworkRepository
{
  public GetListResult GetList(string schemaName, string entityName,
    string fields = "", string filter = "", string sort = "", int limit = 0, int offset = 0)
  {
    SelectListStatement select = this.sqlDialect.BuildGetListSql(schemaName, entityName, fields, filter, sort, limit, offset);
    using ReaderConnection connect = this.sqlDialect.GetReaderConnection();
    IEnumerable<dynamic> found = connect.Connection.Query(select.Sql, select.Parameters);
    long count = connect.Connection.ExecuteScalar<long>(select.CountSql, select.Parameters);

    string lastId = found.Any()
      ? this.sqlDialect.GetPkValue(schemaName, entityName, found.Last())
      : string.Empty;

    return new GetListResult(found.ToList(), count, lastId, PagingToken.ParseLimit(limit), offset);
  }

}
