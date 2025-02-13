using Json.Patch;
using Patchwork.Extensions;
using Patchwork.SqlStatements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Patchwork.Tests.Extensions;

public class JsonPatchExtensions_Tests
{
    private JsonPatch updates;
    private JsonPatch inserts;
    private JsonPatch deletes;
    public JsonPatchExtensions_Tests()
    {
        updates = JsonSerializer.Deserialize<JsonPatch>("[{\"op\": \"replace\",\"path\": \"/ME_9997/productName\",\"value\": \"2025 Factory 5 MK2 Roadster\"},{\"op\": \"replace\",\"path\": \"/ME_9999/productScale\",\"value\": \"1:15\"},{\"op\": \"replace\",\"path\": \"/ME_9999/productVendor\",\"value\": \"Autoart Studio Design\"}]");
        inserts = JsonSerializer.Deserialize<JsonPatch>("[{\"op\": \"add\",\"path\": \"/-\",\"value\": \"2025 Factory 5 MK2 Roadster\"},{\"op\": \"add\",\"path\": \"/-\",\"value\": \"1:15\"},{\"op\": \"add\",\"path\": \"/-\",\"value\": \"Autoart Studio Design\"}]");
        deletes = JsonSerializer.Deserialize<JsonPatch>("[{\"op\": \"remove\",\"path\": \"/ME_9997\"},{\"op\": \"remove\",\"path\": \"/ME_9998\"},{\"op\": \"remove\",\"path\": \"/ME_9999\"}]");
    }
    [Fact]
    public void JsonPatchExtensions_ShouldSplitPatchById_WhenPatchHasUpdates(){
        Dictionary<string, JsonPatch> output = updates.SplitById();

        Assert.Equal(2, output.Count);

        Assert.Contains<string>("9997", output.Keys);
        Assert.Contains<string>("9999", output.Keys);

        Assert.Equal(1, output["9997"].Operations.Count);
        Assert.Equal(2, output["9999"].Operations.Count);

        Assert.Equal("/productName", output["9997"].Operations[0].Path.ToString());
        Assert.Equal("/productScale", output["9999"].Operations[0].Path.ToString());
        Assert.Equal("/productVendor", output["9997"].Operations[1].Path.ToString());
    }
    [Fact]
    public void JsonPatchExtensions_ShouldSplitPatchById_WhenPatchHasInserts()
    {
        Dictionary<string, JsonPatch> output = inserts.SplitById();

        Assert.Equal(3, output.Count);
    }
    [Fact]
    public void JsonPatchExtensions_ShouldSplitPatchById_WhenPatchHasDeletes()
    {

    }
}
