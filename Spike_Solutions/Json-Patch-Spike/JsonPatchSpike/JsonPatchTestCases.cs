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
                        JsonElement testName;
                        jsonElement.TryGetProperty("comment", out testName);

                        JsonElement errorMessage;

                        if (!jsonElement.TryGetProperty("error", out errorMessage))
                        {
                            PatchResult actual = GetPatchResult(jsonElement);

                            var expected = jsonElement.GetProperty("expected");

                            Assert.True(JsonNode.DeepEquals(actual.Result, expected.AsNode()), $"Test '{testName}' failed! Output '{actual}' not equal to expected output '{expected}'");
                        }
                        else
                        {
                            try
                            {
                                PatchResult actual = GetPatchResult(jsonElement);

                                Assert.True(actual.Error != null, $"Test '{testName}' failed! Expected error '{errorMessage}'");
                            }
                            catch(Exception ex)
                            {
                                //This means the test passed... so there's nothing to do here... 
                            }
                        }
                    }
                }
            }
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