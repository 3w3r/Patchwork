using System.Text.Json;
using Dapper;
using Json.Patch;
using Patchwork.SqlDialects;
using Patchwork.SqlStatements;
using static Dapper.SqlMapper;

namespace Patchwork.Repository;
public partial class PatchworkRepository : IPatchworkRepository
{
  public GetResourceResult GetResource(string schemaName, string entityName,
    string id, string fields = "", string include = "")
  {
    //TODO: Need to implement repacking of the result into an object hierarchy breakdown instead of flat record results.
    if (!string.IsNullOrEmpty(include))
      throw new NotImplementedException();

    SelectResourceStatement select = this.sqlDialect.BuildGetSingleSql(schemaName, entityName, id, fields, include);
    using ReaderConnection connect = this.sqlDialect.GetReaderConnection();
    IEnumerable<dynamic> found = connect.Connection.Query(select.Sql, select.Parameters);

    if (found.Count() == 1)
      return new GetResourceResult(found.FirstOrDefault());
    else
      //TODO: Instead of returning the list, we need to convert the flat records into an object hierarchy
      return new GetResourceResult(found.ToList());
  }
  public GetResourceAsOfResult GetResourceAsOf(string schemaName, string entityName, string id, DateTimeOffset asOf)
  {
    SelectEventLogStatement select = this.sqlDialect.BuildGetEventLogSql(schemaName, entityName, id, asOf);
    using ReaderConnection connect = this.sqlDialect.GetReaderConnection();
    IEnumerable<PatchworkLogEvent> found = connect.Connection.Query<PatchworkLogEvent>(select.Sql, select.Parameters);
    JsonDocument resource = JsonDocument.Parse("{}");

    foreach (PatchworkLogEvent log in found)
    {
      JsonPatch? patch = JsonSerializer.Deserialize<JsonPatch>(log.Patch);
      if (patch == null)
        throw new InvalidDataException($"The JSON Patch could not be read  \n {log}");

      if (patch.Operations.Count == 1)
      {
        PatchOperation op = patch.Operations.First();
        if (op.Op == OperationType.Remove
          && op.Path.Contains(schemaName, StringComparer.OrdinalIgnoreCase)
          && op.Path.Contains(entityName, StringComparer.OrdinalIgnoreCase)
          && op.Path.Contains(id, StringComparer.OrdinalIgnoreCase))
        {
          resource = JsonDocument.Parse("{}");
          continue;
        }
      }

      JsonDocument? changed = patch.Apply(resource);
      if (changed == null)
        throw new InvalidDataException($"The JSON Patch could not be applied \n {log}");

      resource = changed;

    }
    return new GetResourceAsOfResult(resource, found.Count(), found.Last().EventDate);

  }
}
