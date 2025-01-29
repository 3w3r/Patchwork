using Patchwork.SqlDialects;

namespace Patchwork.Tests
{
  public class FilterTokenizer_PostgreSql_Tests
  {
    [Theory]
    [InlineData("ID eq 42",
                "WHERE t_monkeytable.id = 42")]
    [InlineData("First_Name eq 'bill'",
                "WHERE t_monkeytable.first_name = 'bill'")]
    [InlineData("Name eq 'jack' or foo eq 'bar'",
                "WHERE t_monkeytable.name = 'jack' OR t_monkeytable.foo = 'bar'")]
    [InlineData("(ID eq 42 AND Name eq 'Jack') OR (ID      eq    38 AND     Name   eq 'Bill') OR (Name eq 'Susan' AND ID eq 88)",
                "WHERE (t_monkeytable.id = 42 AND t_monkeytable.name = 'Jack') OR (t_monkeytable.id = 38 AND t_monkeytable.name = 'Bill') OR (t_monkeytable.name = 'Susan' AND t_monkeytable.id = 88)")]
    [InlineData("Price gt 10 AND Price lt 40",
                "WHERE t_monkeytable.price > 10 AND t_monkeytable.price < 40")]
    [InlineData("(Price gt 40 OR Price lt 10) AND Name eq 'Widget C'",
                "WHERE (t_monkeytable.price > 40 OR t_monkeytable.price < 10) AND t_monkeytable.name = 'Widget C'")]
    [InlineData("Name ne 'Bill'", "WHERE t_monkeytable.name != 'Bill'")]
    [InlineData("ID gt 30", "WHERE t_monkeytable.id > 30")]
    [InlineData("ID ge 30", "WHERE t_monkeytable.id >= 30")]
    [InlineData("ID lt 30", "WHERE t_monkeytable.id < 30")]
    [InlineData("ID le 30", "WHERE t_monkeytable.id <= 30")]
    [InlineData("Name in ('Bill','Susan','Jack')", "WHERE t_monkeytable.name IN ('Bill', 'Susan', 'Jack')")]
    [InlineData("Name ct 'Bill'", "WHERE t_monkeytable.name ILIKE E'%Bill%'")]
    [InlineData("Name sw 'Bill'", "WHERE t_monkeytable.name ILIKE E'Bill%'")]

    // Filters that contain dates and times
    [InlineData("skillKey eq 'cdl' AND effectiveStartDate le '2023-11-11T22:00:00-0400' AND effectiveEndDate gt '2023-11-12T06:00:00-0400'",
                "WHERE t_monkeytable.skillkey = 'cdl' AND t_monkeytable.effectivestartdate <= '2023-11-12T02:00:00.0000000Z' AND t_monkeytable.effectiveenddate > '2023-11-12T10:00:00.0000000Z'")]
    public void ConvertToSqlWhereClause_HandlesCommonCases(string filterString, string expected)
    {
      // Arrange
      PostgreSqlDialectBuilder sut = new PostgreSqlDialectBuilder(TestSampleData.DB);

      // Act
      string actual = sut.BuildWhereClause(filterString, "MonkeyTable");

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
    [InlineData("FooBar eq 'nope'", "column FooBar does not exist on this table.")]
    public void ConvertToSqlWhereClause_ThrowsException_WhenInputFilterStringIsInvalid(string input, string error)
    {
      // Arrange
      string filterString = input;
      PostgreSqlDialectBuilder sut = new PostgreSqlDialectBuilder(TestSampleData.DB);

      // Act
      ArgumentException ex = Assert.ThrowsAny<ArgumentException>(() => sut.BuildWhereClause(filterString, "MonkeyTable"));

      if (ex == null)
        throw new Exception(error);
    }
  }
}
