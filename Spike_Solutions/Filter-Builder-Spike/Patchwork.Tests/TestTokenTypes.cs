using System.Text.Json;
using Patchwork.Extensions;
using Patchwork.Filters;

namespace Patchwork.Tests;

public class TestTokenTypes
{
  [Fact]
  public void AllTokenTypes_Are_Defined()
  {
    FilterTokenType t = FilterTokenType.Textual;
    Assert.True(FilterTokenType.Value.HasFlag(t));
  }
}

public class TestDictionaryExtensionsMethods
{
  [Fact]
  public void DictionaryExtensionTests_WhenConvertingRandomJson_ShouldCreateDictionaryEntries()
  {
    // Arrange
    var json = JsonDocument.Parse("{\"name\": \"John\", \"age\": 35, \"scores\": [90, 85, 95], \"address\": { \"street\": \"Main St\", \"zip\": 12345 }}");
    var SUT = new Dictionary<string, object>();

    // Act
    SUT.AddJsonResourceToDictionary(json);

    // Assert
    Assert.Contains("name", SUT.Keys);
    Assert.Contains("age", SUT.Keys);
    Assert.Contains("scores", SUT.Keys);
    Assert.Contains("address", SUT.Keys);
  }
}
