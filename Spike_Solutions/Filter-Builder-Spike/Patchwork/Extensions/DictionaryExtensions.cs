using System.Text.Json;
using Patchwork.DbSchema;

namespace Patchwork.Extensions;

public static class DictionaryExtensions
{
  public static void SetParameterDataTypes(this Dictionary<string, object> keyValuePairs, Entity entity)
  {
    foreach (Column col in entity.Columns)
    {
      string? key = col.IsPrimaryKey
                  ? "id"
                  : keyValuePairs.Keys.FirstOrDefault(k => k.Equals(col.Name, StringComparison.OrdinalIgnoreCase));

      if (string.IsNullOrEmpty(key))
        continue;

      keyValuePairs[key] = Convert.ChangeType(keyValuePairs[key], col.DataFormat);
    }
  }

  public static void AddJsonResourceToDictionary(this Dictionary<string, object> keyValuePairs, string jsonResource)
  {
    if (keyValuePairs == null)
      throw new ArgumentNullException(nameof(keyValuePairs));
    if (string.IsNullOrEmpty(jsonResource))
      throw new ArgumentNullException(nameof(jsonResource));

    using JsonDocument? json = JsonSerializer.Deserialize<JsonDocument>(jsonResource);
    if (json == null)
      throw new ArgumentException($"Invalid JSON resource: {jsonResource}", nameof(jsonResource));

    foreach (JsonProperty prop in json.RootElement.EnumerateObject())
    {
      object? obj = ConvertJsonElement(prop.Value);
      if (obj != null)
        keyValuePairs[prop.Name] = obj;
    }
  }

  private static object? ConvertJsonElement(JsonElement element)
  {
    return element.ValueKind switch
    {
      JsonValueKind.String => element.GetString(),
      JsonValueKind.Number => GetNumberValue(element),
      JsonValueKind.True => true,
      JsonValueKind.False => false,
      JsonValueKind.Null => null,
      JsonValueKind.Object => ConvertToDictionary(element),
      JsonValueKind.Array => ConvertToArray(element),
      _ => throw new NotSupportedException($"Unsupported JSON value kind: {element.ValueKind}")
    };
  }

  private static object GetNumberValue(JsonElement element)
  {
    if (element.TryGetInt32(out int intValue))
      return intValue;
    if (element.TryGetInt64(out long longValue))
      return longValue;
    if (element.TryGetDouble(out double doubleValue))
      return doubleValue;
    return element.GetRawText(); // Fallback for decimal/other number types
  }

  private static Dictionary<string, object> ConvertToDictionary(JsonElement element)
  {
    Dictionary<string, object> dict = new Dictionary<string, object>();
    foreach (JsonProperty prop in element.EnumerateObject())
    {
      object? obj = ConvertJsonElement(prop.Value);
      if (obj != null)
        dict[prop.Name] = obj;
    }
    return dict;
  }

  private static List<object> ConvertToArray(JsonElement element)
  {
    List<object> list = new List<object>();
    foreach (JsonElement item in element.EnumerateArray())
    {
      object? obj = ConvertJsonElement(item);
      if (obj != null)
        list.Add(obj);
    }
    return list;
  }
}
