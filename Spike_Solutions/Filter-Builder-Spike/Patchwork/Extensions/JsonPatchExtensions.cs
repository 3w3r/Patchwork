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

        for (int i = 0; i < patch.Operations.Count; i++)
        {
            var pathList = patch.Operations[i].Path.ToString().Split("/").Where(x => !string.IsNullOrEmpty(x)).ToList();
            var id = pathList.First();
            pathList.Remove(id);
            var destination = string.Join('/', pathList.ToArray());

            //if (id == "-") { }

            //JsonDocument nodeOp = patch.Operations[i].ToJsonDocument();

            var newOperation = $"\"path\": \"/{destination}\",";

            if (!string.IsNullOrEmpty(patch.Operations[i].Op.ToString()))
            {
                newOperation += $"\"op\": \"{patch.Operations[i].Op.ToString()}\",";
            }
            if (!string.IsNullOrEmpty(patch.Operations[i].From.ToString()))
            {
                newOperation += $"\"from\": \"{patch.Operations[i].From.ToString()}\",";
            }
            if (!string.IsNullOrEmpty(patch.Operations[i].Value.ToString()))
            {
                newOperation += $"\"value\": \"{patch.Operations[i].Value.ToString()}\",";
            }
            newOperation = "{" + newOperation.TrimEnd(',') + "}";

            if (!patchDictionary.ContainsKey(id)) {
                patchDictionary.Add(id, new List<string>());
            }
            patchDictionary[id].Add(newOperation);


        }

        return new Dictionary<string, JsonPatch>();
    }
}
