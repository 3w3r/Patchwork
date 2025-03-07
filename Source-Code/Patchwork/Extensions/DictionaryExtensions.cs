using System.Text.Json;
using Json.Patch;
using Microsoft.AspNetCore.Http;
using Patchwork.DbSchema;
using Patchwork.Repository;

namespace Patchwork.Extensions;

public static class DictionaryExtensions
{
  /// <summary>
  /// Sets the parameter data types based on the given entity's columns.
  /// </summary>
  /// <param name="keyValuePairs">The dictionary containing the key-value pairs of parameters.</param>
  /// <param name="entity">The entity to use for data type information.</param>
  public static void SetParameterDataTypes(this Dictionary<string, object> keyValuePairs, Entity entity)
  {
    // Iterate through each column in the entity
    foreach (Column col in entity.Columns)
    {
      // Determine the key for the current column in the dictionary
      string? key = col.IsPrimaryKey && keyValuePairs.ContainsKey("id")
                  ? "id"
                  : keyValuePairs.Keys.FirstOrDefault(k => k.Equals(col.Name, StringComparison.OrdinalIgnoreCase));

      // Skip the current column if the key is not found in the dictionary
      if (string.IsNullOrEmpty(key))
        continue;

      // TODO: We may be able to expand this method to handle more complex data types. For example, if the column is
      // a string we may be able to convert it to a Dapper DbType with the appropriate maximum character length. This
      // could avoid unnecessary cast conversions in the database.

      string val = keyValuePairs[key]?.ToString() ?? string.Empty;
      // Switch statement to handle different data types
      switch (col.DataFormat.Name)
      {
        case "Guid":
          // Parse the value as a Guid and assign it to the dictionary
          keyValuePairs[key] = Guid.Parse(val);
          break;
        case "Int32":
          // Parse the value as an int and assign it to the dictionary
          keyValuePairs[key] = int.Parse(val);
          break;
        case "Int64":
          // Parse the value as a long and assign it to the dictionary
          keyValuePairs[key] = long.Parse(val);
          break;
        case "Decimal":
          // Parse the value as a decimal and assign it to the dictionary
          keyValuePairs[key] = decimal.Parse(val);
          break;
        case "DateTime":
          // Parse the value as a DateTime and assign it to the dictionary
          keyValuePairs[key] = DateTime.Parse(val);
          break;
        case "Boolean":
          // Parse the value as a bool and assign it to the dictionary
          keyValuePairs[key] = bool.Parse(val);
          break;
        case "String":
          // Assign the value as a string to the dictionary
          keyValuePairs[key] = val;
          break;
        default:
          // Convert the value to the specified data type and assign it to the dictionary
          keyValuePairs[key] = Convert.ChangeType(keyValuePairs[key], col.DataFormat);
          break;
      }
    }
  }

  /// <summary>
  /// Adds the JSON resource to the dictionary.
  /// </summary>
  /// <param name="keyValuePairs">The dictionary to add the JSON resource to.</param>
  /// <param name="jsonResource">The JSON resource to add.</param>
  /// <exception cref="ArgumentNullException">Thrown when either <paramref name="keyValuePairs"/> or <paramref name="jsonResource"/> is null.</exception>
  public static void AddJsonResourceToDictionary(this Dictionary<string, object> keyValuePairs, JsonDocument jsonResource)
  {
    // Check if the keyValuePairs dictionary is null
    if (keyValuePairs == null)
      throw new ArgumentNullException(nameof(keyValuePairs));

    // Check if the jsonResource is null
    if (jsonResource == null)
      throw new ArgumentNullException(nameof(jsonResource));

    // Iterate through each property in the JSON resource
    foreach (JsonProperty prop in jsonResource.RootElement.EnumerateObject())
    {
      // Convert the JSON element value to an object
      object? obj = ConvertJsonElement(prop.Value);

      // Add the property name and object to the dictionary if the object is not null
      if (obj != null)
        keyValuePairs[prop.Name] = obj;
    }
  }

  /// <summary>
  /// Adds the Content-Range header to the HTTP headers based on the provided GetListResult.
  /// </summary>
  /// <param name="headers">The HTTP headers to add the Content-Range header to.</param>
  /// <param name="found">The GetListResult containing the information to calculate the Content-Range header value.</param>
  public static void AddContentRangeHeader(this IHeaderDictionary headers, GetListResult found)
  {
    // Calculate the page size based on the limit and count properties of the GetListResult
    int pageSize = found.Limit != found.Count ? found.Count : found.Limit;

    // Add the Content-Range header to the HTTP headers with the calculated value
    headers.Append("Content-Range", $"items {found.Offset}-{found.Offset + pageSize}/{found.TotalCount}");
  }

  /// <summary>
  /// Adds the X-Json-Patch-Changes header to the HTTP headers based on the provided JsonPatch.
  /// </summary>
  /// <param name="headers">The HTTP headers to add the X-Json-Patch-Changes header to.</param>
  /// <param name="changes">The JsonPatch containing the changes to be applied.</param>
  public static void AddPatchChangesHeader(this IHeaderDictionary headers, JsonPatch changes)
  {
    //TODO: We may want to change this so it has the URL to access the JSON Patch instead of the
    //      actual patch document since this document could be large when there are many changes
    //      in the entity.

    // Serialize the JsonPatch to a JSON string and add it to the HTTP headers with the X-Json-Patch-Changes key
    headers.Append("X-Json-Patch-Changes", JsonSerializer.Serialize(changes));
  }

  /// <summary>
  /// Adds the Date and X-Resource-Version headers to the HTTP headers based on the provided GetResourceAsOfResult.
  /// </summary>
  /// <param name="headers">The HTTP headers to add the headers to.</param>
  /// <param name="result">The GetResourceAsOfResult containing the information to populate the headers.</param>
  public static void AddDateAndResourceVersionHeader(this IHeaderDictionary headers, GetResourceAsOfResult result)
  {
    // Add the Date header to the HTTP headers with the current date and time in UTC format
    headers.Append("Date", result.AsOf.ToUniversalTime().ToString("O"));

    // Add the X-Resource-Version header to the HTTP headers with the version of the resource
    headers.Append("X-Resource-Version", result.Version.ToString());
  }

  private static object? ConvertJsonElement(JsonElement element)
  {
    // Switch statement to handle different JSON value kinds
    return element.ValueKind switch
    {
      JsonValueKind.String => element.GetString(),          // Convert string value to string
      JsonValueKind.Number => GetNumberValue(element),      // Convert number value to appropriate numeric type
      JsonValueKind.True => true,                           // Convert boolean true value to bool
      JsonValueKind.False => false,                         // Convert boolean false value to bool
      JsonValueKind.Null => null,                           // Convert null value to null
      JsonValueKind.Object => ConvertToDictionary(element), // Convert object value to dictionary
      JsonValueKind.Array => ConvertToArray(element),       // Convert array value to list
      // Throw exception for unsupported JSON value kinds
      _ => throw new NotSupportedException($"Unsupported JSON value kind: {element.ValueKind}")
    };
  }

  private static object GetNumberValue(JsonElement element)
  {
    // Try to parse the JSON element value as an int
    if (element.TryGetInt32(out int intValue))
      return intValue;

    // Try to parse the JSON element value as a long
    if (element.TryGetInt64(out long longValue))
      return longValue;

    // Try to parse the JSON element value as a double
    if (element.TryGetDouble(out double doubleValue))
      return doubleValue;

    // Fallback for decimal/other number types: return the raw text of the JSON element value
    return element.GetRawText();
  }

  private static Dictionary<string, object> ConvertToDictionary(JsonElement element)
  {
    // Create a new dictionary to store the converted JSON object properties
    Dictionary<string, object> dict = new Dictionary<string, object>();

    // Iterate through each property in the JSON object
    foreach (JsonProperty prop in element.EnumerateObject())
    {
      // Convert the property value to an object and add it to the dictionary if it's not null
      object? obj = ConvertJsonElement(prop.Value);
      if (obj != null)
        dict[prop.Name] = obj;
    }

    // Return the created dictionary
    return dict;
  }

  private static List<object> ConvertToArray(JsonElement element)
  {
    // Create a new list to store the converted JSON array elements
    List<object> list = new List<object>();

    // Iterate through each element in the JSON array
    foreach (JsonElement item in element.EnumerateArray())
    {
      // Convert the element value to an object and add it to the list if it's not null
      object? obj = ConvertJsonElement(item);
      if (obj != null)
        list.Add(obj);
    }

    // Return the created list
    return list;
  }
}
