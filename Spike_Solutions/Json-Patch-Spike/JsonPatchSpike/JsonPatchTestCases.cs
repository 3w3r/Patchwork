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

            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream!))
            {
                string jsonContent = reader.ReadToEnd();
                JsonDocument jsonDocument = JsonDocument.Parse(jsonContent);
                Assert.NotNull(jsonDocument?.RootElement);

                foreach (var jsonElement in jsonDocument.RootElement.EnumerateArray())
                {
                    JsonElement disabled;
                    if (!jsonElement.TryGetProperty("disabled", out disabled))
                    {
                        JsonElement errorMessage;

                        if (!jsonElement.TryGetProperty("error", out errorMessage))
                        {
                            EnsurePasses(jsonElement);
                        }
                        else
                        {
                            EnsureFails(jsonElement, errorMessage);
                        }
                    }
                }
            }
        }

        private static void EnsurePasses(JsonElement jsonElement)
        {
            JsonElement testName;
            jsonElement.TryGetProperty("comment", out testName);

            PatchResult actual = GetPatchResult(jsonElement);

            var expected = jsonElement.GetProperty("expected");

            Assert.True(JsonNode.DeepEquals(actual.Result, expected.AsNode()), $"Test '{testName}' failed! Output '{actual}' not equal to expected output '{expected}'");
        }

        private static void EnsureFails(JsonElement jsonElement, JsonElement errorMessage)
        {
            JsonElement testName;
            jsonElement.TryGetProperty("comment", out testName);

            Assert.ThrowsAny<Exception>(() => {

                PatchResult actual = GetPatchResult(jsonElement);

                if (actual.Error != null)
                {
                    throw new Exception(actual.Error);
                }
            });
        }

        private static PatchResult GetPatchResult(JsonElement jsonElement)
        {
            JsonNode? doc = JsonNode.Parse(jsonElement.GetProperty("doc").ToString());
            Assert.NotNull(doc);
            var patch = JsonSerializer.Deserialize<JsonPatch>(jsonElement.GetProperty("patch"));

            Assert.NotNull(patch);
            Assert.NotNull(doc);

            var actual = patch.Apply(doc);
            return actual;
        }
    }
}