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
    private JsonPatch combined;
    public JsonPatchExtensions_Tests()
    {
        updates = JsonSerializer.Deserialize<JsonPatch>("[{\"op\": \"replace\",\"path\": \"/ME_9997/productName\",\"value\": \"2025 Factory 5 MK2 Roadster\"},{\"op\": \"replace\",\"path\": \"/ME_9999/productScale\",\"value\": \"1:15\"},{\"op\": \"replace\",\"path\": \"/ME_9999/productVendor\",\"value\": \"Autoart Studio Design\"}]") ?? new JsonPatch();
        inserts = JsonSerializer.Deserialize<JsonPatch>("[{\"op\": \"add\",\"path\": \"/-\",\"value\": \"2025 Factory 5 MK2 Roadster\"},{\"op\": \"add\",\"path\": \"/-\",\"value\": \"1:15\"},{\"op\": \"add\",\"path\": \"/-\",\"value\": \"Autoart Studio Design\"}]") ?? new JsonPatch();
        deletes = JsonSerializer.Deserialize<JsonPatch>("[{\"op\": \"remove\",\"path\": \"/ME_9997\"},{\"op\": \"remove\",\"path\": \"/ME_9998\"},{\"op\": \"remove\",\"path\": \"/ME_9999\"}]") ?? new JsonPatch();
        combined = JsonSerializer.Deserialize<JsonPatch>("[" +
            "{\"op\": \"remove\",\"path\": \"/ME_9997\"},{\"op\": \"remove\",\"path\": \"/ME_9998\"},{\"op\": \"remove\",\"path\": \"/ME_9999\"}," +
            "{\"op\": \"add\",\"path\": \"/-\",\"value\": \"2025 Factory 5 MK2 Roadster\"},{\"op\": \"add\",\"path\": \"/-\",\"value\": \"1:15\"},{\"op\": \"add\",\"path\": \"/-\",\"value\": \"Autoart Studio Design\"}," +
            "{\"op\": \"replace\",\"path\": \"/ME_9997/productName\",\"value\": \"2025 Factory 5 MK2 Roadster\"},{\"op\": \"replace\",\"path\": \"/ME_9999/productScale\",\"value\": \"1:15\"},{\"op\": \"replace\",\"path\": \"/ME_9999/productVendor\",\"value\": \"Autoart Studio Design\"}" +
            "]") ?? new JsonPatch();
    }
    [Fact]
    public void JsonPatchExtensions_ShouldSplitPatchById_WhenPatchHasUpdates(){
        Dictionary<string, JsonPatch> output = updates.SplitById();

        Assert.Equal(2, output.Count);

        Assert.Contains<string>("ME_9997", output.Keys);
        Assert.Contains<string>("ME_9999", output.Keys);

        Assert.Equal(1, output["ME_9997"].Operations?.Count);
        Assert.Equal(2, output["ME_9999"].Operations.Count);

        var ME_9997Paths = output["ME_9997"].Operations.Select(a => { return a.Path.ToString(); });
        Assert.Contains<string>("/productName", ME_9997Paths);

        var ME_9999Paths = output["ME_9999"].Operations.Select(a => { return a.Path.ToString(); });
        Assert.Contains<string>("/productScale", ME_9999Paths);
        Assert.Contains<string>("/productVendor", ME_9999Paths);
    }
    [Fact]
    public void JsonPatchExtensions_ShouldSplitPatchById_WhenPatchHasInserts()
    {
        Dictionary<string, JsonPatch> output = inserts.SplitById();

        Assert.Equal(3, output.Count);

        Assert.Equal("2025 Factory 5 MK2 Roadster", output["-0"].Operations[0]?.Value!.ToString());
        Assert.Equal("1:15", output["-1"].Operations[0]?.Value!.ToString());
        Assert.Equal("Autoart Studio Design", output["-2"]?.Operations[0].Value!.ToString());
    }
    [Fact]
    public void JsonPatchExtensions_ShouldSplitPatchById_WhenPatchHasDeletes()
    {
        Dictionary<string, JsonPatch> output = deletes.SplitById();

        Assert.Equal(3, output.Count);

        Assert.Equal("Remove", output["ME_9997"].Operations[0].Op.ToString());
        Assert.Equal("Remove", output["ME_9998"].Operations[0].Op.ToString());
        Assert.Equal("Remove", output["ME_9999"].Operations[0].Op.ToString());

    }

    [Fact]
    public void JsonPatchExtensions_ShouldSplitPatchById_WhenPatchHasMultipleOperationTypes()
    {
        Dictionary<string, JsonPatch> output = combined.SplitById();

        Assert.Equal(6, output.Count);

        Assert.Contains<string>("ME_9997", output.Keys);
        Assert.Contains<string>("ME_9998", output.Keys);
        Assert.Contains<string>("ME_9999", output.Keys);
        Assert.Contains<string>("-0", output.Keys);
        Assert.Contains<string>("-1", output.Keys);
        Assert.Contains<string>("-2", output.Keys);

        Assert.Equal(2, output["ME_9997"].Operations.Count);
        Assert.Equal(1, output["ME_9998"].Operations?.Count);
        Assert.Equal(3, output["ME_9999"].Operations.Count);
        Assert.Equal(1, output["-0"].Operations?.Count);
        Assert.Equal(1, output["-1"].Operations?.Count);
        Assert.Equal(1, output["-2"].Operations?.Count);


        var ME_9997Paths = output["ME_9997"].Operations.Select(a => { return a.Path.ToString(); });
        Assert.Contains<string>("/productName", ME_9997Paths);

        var ME_9999Paths = output["ME_9999"].Operations.Select(a => { return a.Path.ToString(); });
        Assert.Contains<string>("/productScale", ME_9999Paths);
        Assert.Contains<string>("/productVendor", ME_9999Paths);


        var ME_9997Operations = output["ME_9997"].Operations.Select(a => { return a.Op.ToString(); });
        Assert.Contains<string>("Remove", ME_9997Operations);
        Assert.Contains<string>("Replace", ME_9997Operations);
        Assert.DoesNotContain<string>("add", ME_9997Operations);

        var ME_9998Operations = output["ME_9998"].Operations.Select(a => { return a.Op.ToString(); });
        Assert.Contains<string>("Remove", ME_9998Operations);
        Assert.DoesNotContain<string>("Replace", ME_9998Operations);
        Assert.DoesNotContain<string>("add", ME_9998Operations);

        var ME_9999Operations = output["ME_9999"].Operations.Select(a => { return a.Op.ToString(); });
        Assert.Contains<string>("Remove", ME_9999Operations);
        Assert.Contains<string>("Replace", ME_9999Operations);
        Assert.DoesNotContain<string>("add", ME_9999Operations);


        Assert.Equal("2025 Factory 5 MK2 Roadster", output["-0"].Operations[0]?.Value!.ToString());
        Assert.Equal("1:15", output["-1"].Operations[0]?.Value!.ToString());
        Assert.Equal("Autoart Studio Design", output["-2"].Operations[0]?.Value!.ToString());

    }
}
