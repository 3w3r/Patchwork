using System.Text.Json;
using Json.Patch;

namespace Patchwork.Repository;

public record GetListResult(List<dynamic> Resources, long TotalCount, string LastId, int Limit, int Offset)
{
  public int Count => Resources?.Count ?? 0;
}
public record GetResourceResult(dynamic Resource);
public record GetResourceAsOfResult(JsonDocument Resource, int Version, DateTimeOffset AsOf);
public record PostResult(string Id, dynamic Resource, JsonPatch Changes);
public record PutResult(dynamic Resource, JsonPatch Changes);
public record DeleteResult(bool Success, string Id, JsonPatch? Changes);
public record PatchResourceResult(string Id, dynamic Resource, JsonPatch Changes);
public record PatchDeleteResult(string Id, JsonPatch Changes);
public record PatchListResult(List<PatchResourceResult> Created, List<PatchResourceResult> Updated, List<PatchDeleteResult> Deleted);
