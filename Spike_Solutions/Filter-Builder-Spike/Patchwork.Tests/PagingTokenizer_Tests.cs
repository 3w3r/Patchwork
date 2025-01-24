using Patchwork.Paging;

namespace Patchwork.Tests;

public class PagingTokenizer_Tests
{
  [Theory]
  [InlineData(0, 0, "OFFSET 0 ROWS FETCH NEXT 25 ROWS ONLY")]
  [InlineData(-5, 0, "OFFSET 0 ROWS FETCH NEXT 25 ROWS ONLY")]
  [InlineData(0, -59, "OFFSET 0 ROWS FETCH NEXT 25 ROWS ONLY")]
  [InlineData(-10, -123, "OFFSET 0 ROWS FETCH NEXT 25 ROWS ONLY")]
  [InlineData(10, 0, "OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY")]
  [InlineData(-123, 1337, "OFFSET 1337 ROWS FETCH NEXT 25 ROWS ONLY")]
  [InlineData(30, 1974, "OFFSET 1974 ROWS FETCH NEXT 30 ROWS ONLY")]
  [InlineData(4999, 1974, "OFFSET 1974 ROWS FETCH NEXT 4999 ROWS ONLY")]
  [InlineData(2245816, 22788956, "OFFSET 22788956 ROWS FETCH NEXT 5000 ROWS ONLY")]
  public void Parse_ReturnsOffsetString_ForMsSql(int limit, int offset, string expected)
  {
    // Arrange
    PagingToken tokenizer = new PagingToken(limit, offset);
    MsSqlPagingParser parser = new MsSqlPagingParser(tokenizer);

    // Act
    string result = parser.Parse();

    // Assert
    Assert.Equal(expected, result);
  }

  [Theory]
  [InlineData(0, 0, "LIMIT 25 OFFSET 0")]
  [InlineData(-5, 0, "LIMIT 25 OFFSET 0")]
  [InlineData(0, -59, "LIMIT 25 OFFSET 0")]
  [InlineData(-10, -123, "LIMIT 25 OFFSET 0")]
  [InlineData(10, 0, "LIMIT 10 OFFSET 0")]
  [InlineData(-123, 1337, "LIMIT 25 OFFSET 1337")]
  [InlineData(30, 1974, "LIMIT 30 OFFSET 1974")]
  [InlineData(4999, 1974, "LIMIT 4999 OFFSET 1974")]
  [InlineData(2245816, 22788956, "LIMIT 5000 OFFSET 22788956")]
  public void Parse_ReturnsOffsetString_ForPostgreSql(int limit, int offset, string expected)
  {
    // Arrange
    PagingToken tokenizer = new PagingToken(limit, offset);
    PostgreSqlPagingParser parser = new PostgreSqlPagingParser(tokenizer);

    // Act
    string result = parser.Parse();

    // Assert
    Assert.Equal(expected, result);
  }
}
