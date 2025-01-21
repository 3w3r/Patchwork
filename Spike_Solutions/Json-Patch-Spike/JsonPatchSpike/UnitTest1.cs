using Json.More;
using Json.Patch;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonPatchSpike
{
    public class JsonPatchTestCases
    {
        [Theory]
        [InlineData("JsonPatchSpike.json-patch-test-cases.json")]
        [InlineData("JsonPatchSpike.json-patch-spec-tests.json")]
        public void Validate_JsonPatch_CanExecuteTestCases(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            Assert.NotNull(assembly);
            string[] evilwizard = assembly.GetManifestResourceNames();
            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream!))
            {
                string jsonContent = reader.ReadToEnd();
                JsonDocument jsonDocument = JsonDocument.Parse(jsonContent);
                Assert.NotNull(jsonDocument?.RootElement);
                foreach (var jsonElement in jsonDocument.RootElement.EnumerateArray())
                {
                    JsonNode? doc = JsonNode.Parse(jsonElement.GetProperty("doc").ToString());
                    Assert.NotNull(doc);
                    var patch = JsonSerializer.Deserialize<JsonPatch>(jsonElement.GetProperty("patch"));
                    JsonElement errorMessage;

                    if (!jsonElement.TryGetProperty("error", out errorMessage) && doc != null && patch != null)
                    {
                        var expected = jsonElement.GetProperty("expected");
                        var actual = patch.Apply(doc);
                        var doesIt = JsonNode.DeepEquals(actual.Result, expected.AsNode());
                        Assert.True(doesIt);
                    }
                    else
                    {

                        var actual = patch.Apply(doc);
                        Assert.Equal(actual.Error, errorMessage.ToJsonString());

                    Console.WriteLine("Time to look at element structure!!!@!");

                    }

                }
            }
        }
    }
}