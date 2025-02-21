using Json.Patch;
using Microsoft.AspNetCore.Http;
using Patchwork.Repository;
using System.Text.Json;

namespace Patchwork.Controllers;

public static class ControllerExtensions
{
  public static void AddContentRangeHeader(this IHeaderDictionary headers, GetListResult found)
  {
    int pageSize = found.Limit != found.Count ? found.Count : found.Limit;
    headers.Append("Content-Range", $"items {found.Offset}-{found.Offset + pageSize}/{found.TotalCount}");
  }
  public static void AddPatchChangesHeader(this IHeaderDictionary headers, JsonPatch changes)
  {
    headers.Append("X-Json-Patch-Changes", JsonSerializer.Serialize(changes));
  }
}
