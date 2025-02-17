using Json.More;
using Json.Patch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Patchwork.Extensions;

public static class JsonPatchExtensions
{
    public static Dictionary<string, JsonPatch> SplitById(this JsonPatch patch) {
        
        Dictionary<string, List<string>> patchDictionary = new Dictionary<string, List<string>>();
        var insertCounter = 0;

        foreach (var operation in patch.Operations)
        {
            var pathList = operation.Path.ToString().Split("/").Where(x => !string.IsNullOrEmpty(x)).ToList();
            var id = pathList.First() == "-" ? $"-{insertCounter++}" : pathList.First();
            pathList.Remove(id);
            var destination = string.Join('/', pathList.ToArray());

            var newOperation = $"\"path\": \"/{destination}\",";
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

            if (!patchDictionary.ContainsKey(id)) {
                patchDictionary.Add(id, new List<string>());
            }
            patchDictionary[id].Add(newOperation);

        }

        Dictionary<string, JsonPatch> returnableDictionary = new Dictionary<string, JsonPatch>();

        foreach (var x in patchDictionary)
        {
            var asString = "[" + string.Join(",", x.Value) + "]";
            var asPatch = JsonSerializer.Deserialize<JsonPatch>(asString);
            if(asPatch != null)
                returnableDictionary.Add( x.Key, asPatch );
        }

        return returnableDictionary;
    }
}
