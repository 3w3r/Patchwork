using Json.Patch;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

using Patchwork.Controllers;
using Patchwork.Repository;

using System.Text.Json;


namespace Patchwork.Tests.Controllers;

public class ControllerExtensionTests
{
  [Fact]
  public void AddParentIdToFilterIfNeeded_AddsParentIdFilter_WhenParentIdIsNotNullOrEmpty()
  {
    // Arrange
    string filter = "name eq 'John'";
    string parentId = "123";
    string parentColumnName = "parentId";

    // Act
    string updatedFilter = filter.AddParentIdToFilterIfNeeded(parentId, parentColumnName);

    // Assert
    Assert.Equal("(name eq 'John') AND parentId eq '123'", updatedFilter);
  }

  [Fact]
  public void AddParentIdToFilterIfNeeded_DoesNotAddParentIdFilter_WhenParentIdIsNullOrEmpty()
  {
    // Arrange
    string filter = "name eq 'John'";
    string parentId = "";
    string parentColumnName = "parentId";

    // Act
    string updatedFilter = filter.AddParentIdToFilterIfNeeded(parentId, parentColumnName);

    // Assert
    Assert.Equal("name eq 'John'", updatedFilter);
  }

  [Fact]
  public void AddParentIdToFilterIfNeeded_AddsParentIdFilter_WhenFilterIsEmpty()
  {
    // Arrange
    string filter = "";
    string parentId = "123";
    string parentColumnName = "parentId";

    // Act
    string updatedFilter = filter.AddParentIdToFilterIfNeeded(parentId, parentColumnName);

    // Assert
    Assert.Equal("parentId eq '123'", updatedFilter);
  }


  [Fact]
  public void AddContentRangeHeader_AddsCorrectContentRangeHeader()
  {
    // Arrange
    var headersMock = new MockHeaderDictionary();
    var resources = new List<dynamic>();
    for (int i = 10; i < 30; i++)
      resources.Add(new { Id = i });
    var result = new GetListResult(resources, 100, "30", 20, 10);

    // Act
    headersMock.AddContentRangeHeader(result);

    // Assert
    Assert.Equal("items 10-30/100", headersMock["Content-Range"]);
  }

  [Fact]
  public void AddContentRangeHeader_AddsCorrectContentRangeHeader_WhenLimitAndCountAreDifferent()
  {
    // Arrange
    var headersMock = new MockHeaderDictionary();
    var resources = new List<dynamic>();
    for (int i = 10; i < 60; i++)
      resources.Add(new { Id = i });
    var result = new GetListResult(resources, 100, "60", 50, 10);

    // Act
    headersMock.AddContentRangeHeader(result);

    // Assert
    Assert.Equal("items 10-60/100", headersMock["Content-Range"]);
  }

  [Fact]
  public void AddContentRangeHeader_AddsCorrectContentRangeHeader_WhenOffsetIsZero()
  {
    // Arrange
    var headersMock = new MockHeaderDictionary();
    var resources = new List<dynamic>();
    for (int i = 0; i < 20; i++)
      resources.Add(new { Id = i });
    var result = new GetListResult(resources, 100, "20", 20, 0);

    // Act
    headersMock.AddContentRangeHeader(result);

    // Assert
    Assert.Equal("items 0-20/100", headersMock["Content-Range"]);
  }

  [Fact]
  public void AddDateAndRevisionHeader_AddsCorrectHeaders_WhenResultProvided()
  {
    // Arrange
    IHeaderDictionary headersMock = new MockHeaderDictionary();
    var testDate = DateTimeOffset.Parse("2025-1-1");
    var testVersion = 42;
    var result = new GetResourceAsOfResult(JsonDocument.Parse("{}"), testVersion, testDate);

    // Act
    headersMock.AddDateAndRevisionHeader(result);

    // Assert
    Assert.Equal(testDate, DateTimeOffset.Parse(headersMock["Date"]!));
    Assert.Equal(42, int.Parse(headersMock["X-Resource-Version"]!));
  }

  [Fact]
  public void AddPatchChangesHeader_AddsPatchChangesHeader_WhenChangesProvided()
  {
    // Arrange
    IHeaderDictionary headersMock = new MockHeaderDictionary();
    var changes = JsonSerializer.Deserialize<JsonPatch>("[{\"op\":\"add\",\"path\":\"/name\",\"value\":\"John\"}]");

    // Act
    headersMock.AddPatchChangesHeader(changes!);

    // Assert
    Assert.Equal("[{\"op\":\"add\",\"path\":\"/name\",\"value\":\"John\"}]", headersMock["X-Json-Patch-Changes"]);
  }


  [Fact]
  public void GetLimitFromRangeHeader_ReturnsCorrectLimit_WhenRangeHeaderPresent()
  {
    // Arrange
    IHeaderDictionary headersMock = new MockHeaderDictionary();
    headersMock["Range"] = new StringValues("items=0-100");

    // Act
    var limit = headersMock.GetLimitFromRangeHeader();

    // Assert
    Assert.Equal(100, limit);
  }

  [Fact]
  public void GetLimitFromRangeHeader_ReturnsZero_WhenRangeHeaderNotPresent()
  {
    // Arrange
    IHeaderDictionary headersMock = new MockHeaderDictionary();

    // Act
    var limit = headersMock.GetLimitFromRangeHeader();

    // Assert
    Assert.Equal(0, limit);
  }

  [Fact]
  public void GetLimitFromRangeHeader_ReturnsZero_WhenRangeHeaderInvalid()
  {
    // Arrange
    IHeaderDictionary headersMock = new MockHeaderDictionary();
    headersMock["Range"] = new StringValues("bytes=abc-def");

    // Act
    var limit = headersMock.GetLimitFromRangeHeader();

    // Assert
    Assert.Equal(0, limit);
  }


  [Fact]
  public void GetOffsetFromRangeHeader_ReturnsCorrectOffset_WhenRangeHeaderPresent()
  {
    // Arrange
    IHeaderDictionary headersMock = new MockHeaderDictionary();
    headersMock["Range"] = new StringValues("bytes=10-200");

    // Act
    var offset = headersMock.GetOffsetFromRangeHeader();

    // Assert
    Assert.Equal(10, offset);
  }

  [Fact]
  public void GetOffsetFromRangeHeader_ReturnsZero_WhenRangeHeaderNotPresent()
  {
    // Arrange
    IHeaderDictionary headersMock = new MockHeaderDictionary();

    // Act
    var offset = headersMock.GetOffsetFromRangeHeader();

    // Assert
    Assert.Equal(0, offset);
  }

  [Fact]
  public void GetOffsetFromRangeHeader_ReturnsZero_WhenRangeHeaderInvalid()
  {
    // Arrange
    IHeaderDictionary headersMock = new MockHeaderDictionary();
    headersMock["Range"] = new StringValues("bytes=abc-def");

    // Act
    var offset = headersMock.GetOffsetFromRangeHeader();

    // Assert
    Assert.Equal(0, offset);
  }
}


public class MockHeaderDictionary : Dictionary<string, StringValues>, IHeaderDictionary
{
  public long? ContentLength { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
}
