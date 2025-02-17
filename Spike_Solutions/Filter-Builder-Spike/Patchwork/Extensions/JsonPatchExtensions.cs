using System.Text.Json;
using Json.Patch;

namespace Patchwork.Extensions;

public static class JsonPatchExtensions
{
  public static Dictionary<string, JsonPatch> SplitById(this JsonPatch patch)
  {

    Dictionary<string, List<string>> patchDictionary = new Dictionary<string, List<string>>();
    int insertCounter = 0;

    foreach (PatchOperation operation in patch.Operations)
    {
      List<string> pathList = operation.Path.ToString().Split("/").Where(x => !string.IsNullOrEmpty(x)).ToList();
      string id = pathList.First() == "-" ? $"-{insertCounter++}" : pathList.First();
      pathList.Remove(id);
      string destination = string.Join('/', pathList.ToArray());

      string newOperation = $"\"path\": \"/{destination}\",";
      newOperation += $"\"op\": \"{operation.Op.ToString().ToLower()}\",";

      if (!string.IsNullOrEmpty(operation.From?.ToString()))
      {
        newOperation += $"\"from\": \"{operation.From.ToString()}\",";
      }
      if (!string.IsNullOrEmpty(operation.Value?.ToString()))
      {
        newOperation += $"\"value\": \"{operation.Value.ToString()}\",";
      }
      newOperation = "{" + newOperation.TrimEnd(',') + "}";

      if (!patchDictionary.ContainsKey(id))
      {
        patchDictionary.Add(id, new List<string>());
      }
      patchDictionary[id].Add(newOperation);

    }

    Dictionary<string, JsonPatch> returnableDictionary = new Dictionary<string, JsonPatch>();

    foreach (KeyValuePair<string, List<string>> x in patchDictionary)
    {
      string asString = "[" + string.Join(",", x.Value) + "]";
      JsonPatch? asPatch = JsonSerializer.Deserialize<JsonPatch>(asString);
      if (asPatch != null)
        returnableDictionary.Add(x.Key, asPatch);
    }

    return returnableDictionary;
  }
}
