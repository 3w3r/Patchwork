using Patchwork.SqlDialects;

namespace Patchwork.Filters.Tests
{
  public class FilterTokenizer_PostgreSql_Tests
  {
    [Theory]
    [InlineData("ID eq 42",
                "WHERE ID = 42")]
    [InlineData("Name eq 'jack' or foo eq 'bar'",
                "WHERE Name = 'jack' OR foo = 'bar'")]
    [InlineData("(ID eq 42 AND Name eq 'Jack') OR (ID      eq    38 AND     Name   eq 'Bill') OR (Name eq 'Susan' AND ID eq 88)",
                "WHERE (ID = 42 AND Name = 'Jack') OR (ID = 38 AND Name = 'Bill') OR (Name = 'Susan' AND ID = 88)")]
    [InlineData("Price gt 10 AND Price lt 40",
                "WHERE Price > 10 AND Price < 40")]
    [InlineData("(Price gt 40 OR Price lt 10) AND Name eq 'Widget C'",
                "WHERE (Price > 40 OR Price < 10) AND Name = 'Widget C'")]
    [InlineData("Name ne 'Bill'", "WHERE Name != 'Bill'")]
    [InlineData("ID gt 30", "WHERE ID > 30")]
    [InlineData("ID ge 30", "WHERE ID >= 30")]
    [InlineData("ID lt 30", "WHERE ID < 30")]
    [InlineData("ID le 30", "WHERE ID <= 30")]
    [InlineData("Name in ('Bill','Susan','Jack')", "WHERE Name IN ('Bill', 'Susan', 'Jack')")]
    [InlineData("Name ct 'Bill'", "WHERE Name ILIKE E'%Bill%'")]
    [InlineData("Name sw 'Bill'", "WHERE Name ILIKE E'Bill%'")]

    // Filters that contain dates and times
    [InlineData("skillKey eq 'cdl' AND effectiveStartDate le '2023-11-11T22:00:00-0400' AND effectiveEndDate gt '2023-11-12T06:00:00-0400'",
                "WHERE skillKey = 'cdl' AND effectiveStartDate <= '2023-11-12T02:00:00.0000000Z' AND effectiveEndDate > '2023-11-12T10:00:00.0000000Z'")]
    public void ConvertToSqlWhereClause_HandlesCommonCases(string filterString, string expected)
    {
      // Arrange
      var sut = new PostgreSqlDialectBuilder();

      // Act
      var actual = sut.BuildWhereClause(filterString);

      // Assert
      Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("", "No input string")]
    [InlineData(" ", "No input string")]
    [InlineData("        ", "No input string")]
    [InlineData(" \t\n     ", "No input string")]
    [InlineData("((( ID eq 42", "Invalid parens")]
    [InlineData("(ID eq 42))", "Invalid parens")]
    [InlineData("eq 42", "No column given")]
    [InlineData("ID eq 42 AND", "AND but no second condition")]
    [InlineData(",", "No Tokens in the input string")]
    [InlineData("('1' eq ID)", "Filter conditions MUST the format `property operator value` to be valid")]
    public void ConvertToSqlWhereClause_ThrowsException_WhenInputFilterStringIsInvalid(string input, string error)
    {
      // Arrange
      string filterString = input;
      var sut = new PostgreSqlDialectBuilder();

      // Act
      var ex = Assert.ThrowsAny<ArgumentException>(() => sut.BuildWhereClause(filterString));

      if (ex == null) throw new Exception(error);
    }
  }

  public class TestTokenTypes
  {
    [Fact]
    public void AllTokenTypes_Are_Defined()
    {
      var t = FilterTokenType.Textual;
      Assert.True(FilterTokenType.Value.HasFlag(t));
    }
  }
}
