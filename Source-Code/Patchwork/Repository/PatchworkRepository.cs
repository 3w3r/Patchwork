using System.Text.Json;
using Dapper;
using Json.Patch;
using Patchwork.Authorization;
using Patchwork.SqlDialects;
using Patchwork.SqlStatements;
using static Dapper.SqlMapper;

namespace Patchwork.Repository;

public partial class PatchworkRepository : IPatchworkRepository
{
  protected readonly IPatchworkAuthorization authorization;
  protected readonly ISqlDialectBuilder sqlDialect;
  private readonly JsonDocument empty = JsonDocument.Parse("{}");

  public PatchworkRepository(IPatchworkAuthorization auth, ISqlDialectBuilder sql)
  {
    this.authorization = auth;
    this.sqlDialect = sql;
  }

  public bool AddPatchToLog(WriterConnection connect, HttpMethodsEnum httpMethod, string schemaName, string entityName, string id, JsonPatch patch)
  {
    InsertStatement insertPatch = this.sqlDialect.GetInsertStatementForPatchworkLog(httpMethod, schemaName, entityName, id.ToString(), patch);
    int patchedCount = connect.Connection.Execute(insertPatch.Sql, insertPatch.Parameters, connect.Transaction);
    return patchedCount > 0;
  }
}
