# JSON Patch Tests

In this folder, we have some JSON files that contain test cases for the JSON-Patch operation. The JSON file is an array of test cases where each case demonstrates a specific feature that the JSON-Patch subsystem **MUST** support.

Each test is to evaluate one specific JSON-Patch feature. Some of the test cases intended to demonstrate an invalid JSON-Patch operation and should fail. The failure test cases have a property called `error` while the success cases have a property called `expected`. The `comment` property is the name of the test case.

> NOTE: If you find a test case without a comment, please add one to name that test case.

This is how the cases are set up:

```json
[
  {
    "comment": "A.1.  Adding an Object Member",  // Name of test case
    "doc": {                                     // Original document to be modified
      "foo": "bar"
    },
    "patch": [                                   // JSON Patch that we should apply to
      {                                          // the original document
        "op": "add",
        "path": "/baz",
        "value": "qux"
      }
    ],
    "expected": {                                // Having an `expected` property means
      "baz": "qux",                              // that the patch should succeed and
      "foo": "bar"                               // this should be the result
    },
    {
      "comment": "4.1. add with missing object",   // Name of test case
      "doc": {                                     // Original document to be modified
        "q": {
          "bar": 2
        }
      },
      "patch": [                                   // JSON Patch that we should apply to
        {                                          // the original document
          "op": "add",
          "path": "/a/b",
          "value": 1
        }
      ],
                                                   // Having an `error` property means that
                                                   // this test case should fail to apply
      "error": "path /a does not exist -- missing objects are not created recursively"
    },
  }
]
```

## Unit Test Harness

To execute each of the test cases we will need a Unit Test runner. This Spike Solution contains a single unit test project.

- [ ] The sample JSON-Patch test case documents are added to the unit test project as `embedded resources`.
- [ ] The unit test project uses the xUnit test framework
- [ ] The unit test project imports the Json-Everything package called [JsonPatch.Net](https://docs.json-everything.net/patch/basics/)
- [ ] The project should include a `Theory` test that looks like this:

```CSharp
    [Theory]
    [InlineData("Patchwork.JsonPatchTests.json-patch-test-cases.json")]
    [InlineData("Patchwork.JsonPatchTests.json-patch-spec-tests.json")]
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
          // Execute this test case
        }
      }
    }
```

- [ ] For each case that should succeed, we want to apply the patch and verify that the document matches the expected. That should look something like this:

```CSharp
    private static void EnsurePatchAppliesSuccessfully(JsonElement jsonElement, bool hasComment)
    {
      // Arrange
      string rawComment = jsonElement.GetProperty("comment").GetRawText();
      string rawDoc = jsonElement.GetProperty("doc").GetRawText();
      string rawPatch = jsonElement.GetProperty("patch").GetRawText();
      string rawExpected = jsonElement.GetProperty("expected").GetRawText();

      var doc = JsonDocument.Parse(rawDoc);
      var patch = JsonSerializer.Deserialize<JsonPatch>(rawPatch);
      var expected = JsonNode.Parse(rawExpected);

      // Act
      var actual = JsonNode.Parse((patch?.Apply(doc)).RootElement.GetRawText());

      // Assert
      Assert.True(JsonNode.DeepEquals(expected, actual),
        $"Test failed on {rawComment}, expected {expected} " +
        $"but was {actual}.");
    }
```

- [ ] For each of the test cases that should fail, we want to validate that the `Apply()` method throws and exception. That should look something like this:

```CSharp
    private static void EnsurePatchThrowsException(JsonElement jsonElement)
    {
      // Arrange
      string rawError = jsonElement.GetProperty("error").GetRawText();
      string rawDoc = jsonElement.GetProperty("doc").GetRawText();
      string rawPatch = jsonElement.GetProperty("patch").GetRawText();
      var doc = JsonDocument.Parse(rawDoc);
      var patch = JsonSerializer.Deserialize<JsonPatch>(rawPatch);

      // Act and Assert
      Assert.ThrowsAny<Exception>(() => { patch?.Apply(doc); });
    }
```
