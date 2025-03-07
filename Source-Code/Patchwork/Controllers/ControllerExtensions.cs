using System.Text.Json;
using Json.Patch;
using Microsoft.AspNetCore.Http;
using Patchwork.Repository;

namespace Patchwork.Controllers;

public static class ControllerExtensions
{
  /// <summary>
  /// Adds a parent ID filter to the provided filter string if the parent ID is not empty.
  /// If the filter string is empty, the parent ID filter is set as the filter string.
  /// If the filter string is not empty, the parent ID filter is appended as an additional condition using the AND operator.
  /// </summary>
  /// <param name="filter">The original filter string.</param>
  /// <param name="parentId">The parent ID to add to the filter.</param>
  /// <param name="parentColumnName">The name of the parent column in the database.</param>
  /// <returns>The updated filter string with the parent ID filter added if necessary.</returns>
  public static string AddParentIdToFilterIfNeeded(this string filter, string parentId, string parentColumnName)
  {
    // Checking if the parent ID is not empty
    if (!string.IsNullOrEmpty(parentId))
    {
      // Checking if the filter string is empty
      if (string.IsNullOrEmpty(filter))
      {
        // If the filter string is empty, setting the parent ID filter as the filter string
        filter = $"{parentColumnName} eq '{parentId}'";
      }
      else
      {
        // If the filter string is not empty, appending the parent ID filter as an additional condition using the AND operator
        filter = $"({filter}) AND {parentColumnName} eq '{parentId}'";
      }
    }

    // Returning the updated filter string
    return filter;
  }

  /// <summary>
  /// Adds a Content-Range header to the HTTP response headers based on the provided GetListResult.
  /// The Content-Range header specifies the range of items returned in the response.
  /// If the limit is not equal to the count, the count is used as the page size.
  /// </summary>
  /// <param name="headers">The HTTP response headers.</param>
  /// <param name="result">The GetListResult containing the offset, limit, and total count of items.</param>
  public static void AddContentRangeHeader(this IHeaderDictionary headers, GetListResult result)
  {
    // Calculating the page size based on the limit and count
    int pageSize = result.Limit != result.Count ? result.Count : result.Limit;

    // Adding the Content-Range header to the HTTP response headers
    headers.Append("Content-Range", $"items {result.Offset}-{result.Offset + pageSize}/{result.TotalCount}");
  }

  /// <summary>
  /// Adds a Date and X-Resource-Version header to the HTTP response headers based on the provided GetResourceAsOfResult.
  /// The Date header specifies the current date and time in RFC 1123 format.
  /// The X-Resource-Version header specifies the version of the resource.
  /// </summary>
  /// <param name="headers">The HTTP response headers.</param>
  /// <param name="result">The GetResourceAsOfResult containing the as-of date and version of the resource.</param>
  public static void AddDateAndRevisionHeader(this IHeaderDictionary headers, GetResourceAsOfResult result)
  {
    // Adding the Date header to the HTTP response headers
    headers.Append("Date", result.AsOf.ToUniversalTime().ToString("O"));

    // Adding the X-Resource-Version header to the HTTP response headers
    headers.Append("X-Resource-Version", $"{result.Version}");
  }

  /// <summary>
  /// Adds an X-Json-Patch-Changes header to the HTTP response headers based on the provided JsonPatch.
  /// The X-Json-Patch-Changes header contains the JSON representation of the JsonPatch.
  /// </summary>
  /// <param name="headers">The HTTP response headers.</param>
  /// <param name="changes">The JsonPatch containing the changes to be applied.</param>
  public static void AddPatchChangesHeader(this IHeaderDictionary headers, JsonPatch changes)
  {
    // Adding the X-Json-Patch-Changes header to the HTTP response headers
    headers.Append("X-Json-Patch-Changes", JsonSerializer.Serialize(changes));
  }

  /// <summary>
  /// Retrieves the offset from the Range header in the HTTP request headers.
  /// If the Range header is not present or cannot be parsed, the method returns 0.
  /// </summary>
  /// <param name="headers">The HTTP request headers.</param>
  /// <returns>The offset from the Range header, or 0 if the Range header is not present or cannot be parsed.</returns>
  public static int GetOffsetFromRangeHeader(this IHeaderDictionary headers)
  {
    try
    {
      // Checking if the HTTP request headers contain a "Range" header
      if (headers.ContainsKey("Range"))
      {
        // Extracting the value of the "Range" header
        string? rangeHeader = headers["Range"].First();

        // Splitting the range value into parts based on the "=" and "-" characters
        string[] parts = rangeHeader!.Split('=')[1].Split('-');

        // Parsing the first part of the range value as an integer and returning it
        return int.Parse(parts[0]);
      }
      else
      {
        // If the "Range" header is not present, returning 0
        return 0;
      }
    }
    catch
    {
      // If an exception occurs while parsing the "Range" header, returning 0
      return 0;
    }
  }

  /// <summary>
  /// Retrieves the limit from the Range header in the provided HTTP headers.
  /// </summary>
  /// <param name="headers">The HTTP headers to extract the Range header from.</param>
  /// <returns>The limit specified in the Range header, or 0 if the header is not present or cannot be parsed.</returns>
  public static int GetLimitFromRangeHeader(this IHeaderDictionary headers)
  {
    try
    {
      // Check if the headers contain a Range header
      if (headers.ContainsKey("Range"))
      {
        // Extract the Range header value
        string? rangeHeader = headers["Range"].First();

        // Split the Range header value into parts
        string[] parts = rangeHeader!.Split('=')[1].Split('-');

        // Calculate and return the limit from the Range header
        return int.Parse(parts[1]) - int.Parse(parts[0]);
      }
      else
      {
        // Return 0 if the Range header is not present
        return 0;
      }
    }
    catch
    {
      // Return 0 if an error occurs while parsing the Range header
      return 0;
    }
  }
}
