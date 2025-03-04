using System.Text.Json;
using Patchwork.DbSchema;

namespace Patchwork.Extensions;

public static class DictionaryExtensions
{
  public static void SetParameterDataTypes(this Dictionary<string, object> keyValuePairs, Entity entity)
  {
    foreach (Column col in entity.Columns)
    {
      string? key = col.IsPrimaryKey && keyValuePairs.ContainsKey("id")
                  ? "id"
                  : keyValuePairs.Keys.FirstOrDefault(k => k.Equals(col.Name, StringComparison.OrdinalIgnoreCase));

      if (string.IsNullOrEmpty(key))
        continue;

      // TODO: We may be able to expand this method to handle more complex data types. For example, if the column is
      // a string we may be able to convert it to a Dapper DbType with the appropirate maximium character length. This
      // could avoid unnecessary cast conversions in the database.
      switch (col.DataFormat.Name)
      {
        case "Guid":
          keyValuePairs[key] = Guid.Parse(keyValuePairs[key].ToString());
          break;
        case "Int32":
          keyValuePairs[key] = int.Parse(keyValuePairs[key].ToString());
          break;
        case "Int64":
          keyValuePairs[key] = long.Parse(keyValuePairs[key].ToString());
          break;
        case "Decimal":
          keyValuePairs[key] = decimal.Parse(keyValuePairs[key].ToString());
          break;
        case "DateTime":
          keyValuePairs[key] = DateTime.Parse(keyValuePairs[key].ToString());
          break;
        case "Boolean":
          keyValuePairs[key] = bool.Parse(keyValuePairs[key].ToString());
          break;
        case "String":
          keyValuePairs[key] = keyValuePairs[key].ToString();
          break;
        default:
          keyValuePairs[key] = Convert.ChangeType(keyValuePairs[key], col.DataFormat);
          break;
      }
    }
  }

  public static void AddJsonResourceToDictionary(this Dictionary<string, object> keyValuePairs, JsonDocument jsonResource)
  {
    if (keyValuePairs == null)
      throw new ArgumentNullException(nameof(keyValuePairs));
    if (jsonResource == null)
      throw new ArgumentNullException(nameof(jsonResource));

    foreach (JsonProperty prop in jsonResource.RootElement.EnumerateObject())
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
