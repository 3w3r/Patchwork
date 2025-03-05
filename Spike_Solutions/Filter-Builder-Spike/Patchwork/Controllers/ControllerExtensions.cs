using Json.Patch;
using Microsoft.AspNetCore.Http;
using Patchwork.Repository;
using System.Text.Json;

namespace Patchwork.Controllers;

public static class ControllerExtensions
{
  public static string AddParentIdToFilterIfNeeded(this string filter, string parentId, string parentColumnName)
  {
    if (!string.IsNullOrEmpty(parentId))
    {
      if (string.IsNullOrEmpty(filter))
        filter = $"{parentColumnName} eq '{parentId}'";
      else
        filter = $"({filter}) AND {parentColumnName} eq '{parentId}'";
    }

    return filter;
  }

  public static void AddContentRangeHeader(this IHeaderDictionary headers, GetListResult found)
  {
    int pageSize = found.Limit != found.Count ? found.Count : found.Limit;
    headers.Append("Content-Range", $"items {found.Offset}-{found.Offset + pageSize}/{found.TotalCount}");
  }

  public static void AddPatchChangesHeader(this IHeaderDictionary headers, JsonPatch changes)
  {
    headers.Append("X-Json-Patch-Changes", JsonSerializer.Serialize(changes));
  }

  public static int GetOffsetFromRangeHeader(this IHeaderDictionary headers)
  {
    try
    {
      if (headers.ContainsKey("Range"))
      {
        var rangeHeader = headers["Range"].First();
        string[] parts = rangeHeader!.Split('=')[1].Split('-');
        return int.Parse(parts[0]);
      }
      else
      {
        return 0;
      }
    } catch { return 0; }
  }

  public static int GetLimitFromRangeHeader(this IHeaderDictionary headers)
  {
    try
    { 
    if (headers.ContainsKey("Range"))
    {
      var rangeHeader = headers["Range"].First();
      string[] parts = rangeHeader!.Split('=')[1].Split('-');

      return int.Parse(parts[1]) - int.Parse(parts[0]);
    }
    else
    {
      return 0;
    }
    }
    catch { return 0; }
  }
}
