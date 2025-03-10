using Dapper;
using Patchwork.Paging;
using Patchwork.SqlDialects;
using Patchwork.SqlStatements;
using static Dapper.SqlMapper;

namespace Patchwork.Repository;

public partial class PatchworkRepository : IPatchworkRepository
{
  /// <summary>
  ///   Retrieves a list of records from the specified schema and entity.
  /// </summary>
  /// <param name="schemaName">The name of the database schema.</param>
  /// <param name="entityName">The name of the database entity.</param>
  /// <param name="fields">A comma-separated list of fields to retrieve. If empty, all fields will be returned.</param>
  /// <param name="filter">A SQL WHERE clause to filter the records. If empty, no filtering will be applied.</param>
  /// <param name="sort">A SQL ORDER BY clause to sort the records. If empty, the default sorting will be used.</param>
  /// <param name="limit">The maximum number of records to retrieve. If 0, no limit will be applied.</param>
  /// <param name="offset">The number of records to skip before starting to retrieve the records.</param>
  /// <returns>A GetListResult object containing the retrieved records, total count, last record ID, limit, and offset.</returns>
  public GetListResult GetList(string schemaName, string entityName,
    string fields = "", string filter = "", string sort = "", int limit = 0, int offset = 0)
  {
    // Build the SQL statement for retrieving the list of records
    SelectListStatement select = this.sqlDialect.BuildGetListSql(schemaName, entityName, fields, filter, sort, limit, offset);

    // Get a reader connection to execute the SQL statement
    using ReaderConnection connect = this.sqlDialect.GetReaderConnection();

    // Execute the SQL statement and retrieve the records
    IEnumerable<dynamic> found = connect.Connection.Query(select.Sql, select.Parameters);

    // Execute a separate SQL statement to get the total count of records
    long count = connect.Connection.ExecuteScalar<long>(select.CountSql, select.Parameters);

    // Determine the ID of the last record in the list
    string lastId = found.Any()
      ? this.sqlDialect.GetPkValue(schemaName, entityName, found.Last())
      : string.Empty;

    // Create a GetListResult object and return it
    return new GetListResult(found.ToList(), count, lastId, PagingToken.ParseLimit(limit), offset);
  }
}
