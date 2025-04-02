using System.Text.Json;
using Json.Patch;
using Patchwork.Extensions;

namespace Patchwork.Tests.Extensions;

public class JsonPatchExtensions_Tests
{
  private JsonPatch updates;
  private JsonPatch inserts;
  private JsonPatch deletes;
  private JsonPatch brokenOne;
  private JsonPatch brokenTwo;
  private JsonPatch combined;
  public JsonPatchExtensions_Tests()
  {
    updates = JsonSerializer.Deserialize<JsonPatch>("[{\"op\": \"replace\",\"path\": \"/ME_9997/productName\",\"value\": \"2025 Factory 5 MK2 Roadster\"},{\"op\": \"replace\",\"path\": \"/ME_9999/productScale\",\"value\": \"1:15\"},{\"op\": \"replace\",\"path\": \"/ME_9999/productVendor\",\"value\": \"Autoart Studio Design\"}]") ?? new JsonPatch();
    inserts = JsonSerializer.Deserialize<JsonPatch>("[{\"op\": \"add\",\"path\": \"/-\",\"value\": {\"productName\": \"2025 Factory 5 MK2 Roadster\"}},{\"op\": \"add\",\"path\": \"/-\",\"value\": \"1:15\"},{\"op\": \"add\",\"path\": \"/-\",\"value\": \"Autoart Studio Design\"}]") ?? new JsonPatch();
    deletes = JsonSerializer.Deserialize<JsonPatch>("[{\"op\": \"remove\",\"path\": \"/ME_9997\"},{\"op\": \"remove\", \"path\": \"/ME_9998\"},{\"op\": \"remove\",\"path\": \"/ME_9999\"}]") ?? new JsonPatch();
    brokenOne = JsonSerializer.Deserialize<JsonPatch>("[{\"op\": \"remove\",\"path\": \"/ME_9999\"}, {\"op\": \"remove\",\"path\": \"/ME_9999\"}, {\"op\": \"add\", \"value\": \"Autoart Studio Design\", \"path\": \"/-\"}, {\"op\": \"remove\",\"path\": \"/ME_9995\"}]") ?? new JsonPatch();
    brokenTwo = JsonSerializer.Deserialize<JsonPatch>("[{\"op\": \"add\",\"path\": \"/ME_9997\",\"value\": \"2025 Factory 5 MK2 Roadster\"}]") ?? new JsonPatch();
    combined = JsonSerializer.Deserialize<JsonPatch>("[" +
        "{\"op\": \"remove\",\"path\": \"/ME_1997\"},{\"op\": \"remove\",\"path\": \"/ME_1998\"},{\"op\": \"remove\",\"path\": \"/ME_1999\"}," +
        "{\"op\": \"add\",\"path\": \"/-\",\"value\": \"2025 Factory 5 MK2 Roadster\"},{\"op\": \"add\",\"path\": \"/-\",\"value\": \"1:15\"},{\"op\": \"add\",\"path\": \"/-\",\"value\": \"Autoart Studio Design\"}," +
        "{\"op\": \"replace\",\"path\": \"/ME_2997/productName\",\"value\": \"2025 Factory 5 MK2 Roadster\"},{\"op\": \"replace\",\"path\": \"/ME_2999/productScale\",\"value\": \"1:15\"},{\"op\": \"replace\",\"path\": \"/ME_2999/productVendor\",\"value\": \"Autoart Studio Design\"}" +
        "]") ?? new JsonPatch();
  }
  [Fact]
  public void JsonPatchExtensions_ShouldThrowError_WhenGivenInvalidJsonPatch()
  {
      Assert.ThrowsAny<Exception>(brokenOne.SplitById);
      Assert.ThrowsAny<Exception>(brokenTwo.SplitById);
  }
  [Fact]
  public void JsonPatchExtensions_ShouldSplitPatchById_WhenPatchHasUpdates()
  {
    Dictionary<string, JsonPatch> output = updates.SplitById();

    Assert.Equal(2, output.Count);

    Assert.Contains<string>("ME_9997", output.Keys);
    Assert.Contains<string>("ME_9999", output.Keys);

    Assert.Equal(1, output["ME_9997"].Operations?.Count);
    Assert.Equal(2, output["ME_9999"].Operations.Count);

    IEnumerable<string> ME_9997Paths = output["ME_9997"].Operations.Select(a => { return a.Path.ToString(); });
    Assert.Contains<string>("/productName", ME_9997Paths);

    IEnumerable<string> ME_9999Paths = output["ME_9999"].Operations.Select(a => { return a.Path.ToString(); });
    Assert.Contains<string>("/productScale", ME_9999Paths);
    Assert.Contains<string>("/productVendor", ME_9999Paths);
  }
  [Fact]
  public void JsonPatchExtensions_ShouldSplitPatchById_WhenPatchHasInserts()
  {
    Dictionary<string, JsonPatch> output = inserts.SplitById();

    Assert.Equal(3, output.Count);

    Assert.Equal("{\"productName\":\"2025 Factory 5 MK2 Roadster\"}", output["-0"].Operations[0]?.Value!.ToString());
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

    Assert.Equal(8, output.Count);

    Assert.Contains<string>("ME_1997", output.Keys);
    Assert.Contains<string>("ME_1998", output.Keys);
    Assert.Contains<string>("ME_1999", output.Keys);
    Assert.Contains<string>("ME_2997", output.Keys);
    Assert.Contains<string>("ME_2999", output.Keys);
    Assert.Contains<string>("-0", output.Keys);
    Assert.Contains<string>("-1", output.Keys);
    Assert.Contains<string>("-2", output.Keys);

    Assert.Equal(1, output["ME_1997"].Operations?.Count);
    Assert.Equal(1, output["ME_1998"].Operations?.Count);
    Assert.Equal(1, output["ME_1999"].Operations?.Count);
    Assert.Equal(1, output["ME_2997"].Operations?.Count);
    Assert.Equal(2, output["ME_2999"].Operations?.Count);
    Assert.Equal(1, output["-0"].Operations?.Count);
    Assert.Equal(1, output["-1"].Operations?.Count);
    Assert.Equal(1, output["-2"].Operations?.Count);

    IEnumerable<string> ME_2997Paths = output["ME_2997"].Operations.Select(a => { return a.Path.ToString(); });
    Assert.Contains<string>("/productName", ME_2997Paths);

    IEnumerable<string> ME_2999Paths = output["ME_2999"].Operations.Select(a => { return a.Path.ToString(); });
    Assert.Contains<string>("/productScale", ME_2999Paths);
    Assert.Contains<string>("/productVendor", ME_2999Paths);

    IEnumerable<string> ME_1997Operations = output["ME_1997"].Operations.Select(a => { return a.Op.ToString(); });
    Assert.Contains<string>("Remove", ME_1997Operations);
    Assert.DoesNotContain<string>("Replace", ME_1997Operations);
    Assert.DoesNotContain<string>("add", ME_1997Operations);

    IEnumerable<string> ME_1998Operations = output["ME_1998"].Operations.Select(a => { return a.Op.ToString(); });
    Assert.Contains<string>("Remove", ME_1998Operations);
    Assert.DoesNotContain<string>("Replace", ME_1998Operations);
    Assert.DoesNotContain<string>("add", ME_1998Operations);

    IEnumerable<string> ME_1999Operations = output["ME_1999"].Operations.Select(a => { return a.Op.ToString(); });
    Assert.Contains<string>("Remove", ME_1999Operations);
    Assert.DoesNotContain<string>("Replace", ME_1999Operations);
    Assert.DoesNotContain<string>("add", ME_1999Operations);

    IEnumerable<string> ME_2997Operations = output["ME_2997"].Operations.Select(a => { return a.Op.ToString(); });
    Assert.DoesNotContain<string>("Remove", ME_2997Operations);
    Assert.Contains<string>("Replace", ME_2997Operations);
    Assert.DoesNotContain<string>("add", ME_2997Operations);

    IEnumerable<string> ME_2999Operations = output["ME_2999"].Operations.Select(a => { return a.Op.ToString(); });
    Assert.DoesNotContain<string>("Remove", ME_2999Operations);
    Assert.Contains<string>("Replace", ME_2999Operations);
    Assert.DoesNotContain<string>("add", ME_2999Operations);

    Assert.Equal("2025 Factory 5 MK2 Roadster", output["-0"].Operations[0]?.Value!.ToString());
    Assert.Equal("1:15", output["-1"].Operations[0]?.Value!.ToString());
    Assert.Equal("Autoart Studio Design", output["-2"].Operations[0]?.Value!.ToString());
  }
}
