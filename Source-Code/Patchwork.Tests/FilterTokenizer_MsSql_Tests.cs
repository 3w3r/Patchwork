using Patchwork.SqlDialects.MsSql;

namespace Patchwork.Tests;

public class FilterTokenizer_MsSql_Tests
{
  [Theory]
  [InlineData("ID eq 42",
              "WHERE [t_MonkeyTable].[Id] = @V0", "42", "42", 1)]
  [InlineData("First_Name eq 'bill'",
              "WHERE [t_MonkeyTable].[First_Name] = @V0", "bill", "bill", 1)]
  [InlineData("Name eq 'jack' or foo eq 'bar'",
              "WHERE [t_MonkeyTable].[Name] = @V0 OR [t_MonkeyTable].[FOO] = @V1", "jack", "bar", 2)]
  [InlineData("(ID eq 42 AND Name eq 'Jack') OR (ID      eq    38 AND     Name   eq 'Bill') OR ([Name] eq 'Susan' AND [ID] eq 88)",
              "WHERE ([t_MonkeyTable].[Id] = @V0 AND [t_MonkeyTable].[Name] = @V1) OR ([t_MonkeyTable].[Id] = @V2 AND [t_MonkeyTable].[Name] = @V3) OR ([t_MonkeyTable].[Name] = @V4 AND [t_MonkeyTable].[Id] = @V5)",
              "42", "88", 6)]
  [InlineData("Price gt 10 AND Price lt 40",
              "WHERE [t_MonkeyTable].[Price] > @V0 AND [t_MonkeyTable].[Price] < @V1",
              "10", "40", 2)]
  [InlineData("(Price gt 40 OR Price lt 10) AND Name eq 'Widget C'",
              "WHERE ([t_MonkeyTable].[Price] > @V0 OR [t_MonkeyTable].[Price] < @V1) AND [t_MonkeyTable].[Name] = @V2",
              "40", "Widget C", 3)]
  [InlineData("Name ne 'Bill'", "WHERE [t_MonkeyTable].[Name] != @V0", "Bill", "Bill", 1)]
  [InlineData("ID gt 30", "WHERE [t_MonkeyTable].[Id] > @V0", "30", "30", 1)]
  [InlineData("ID ge 30", "WHERE [t_MonkeyTable].[Id] >= @V0", "30", "30", 1)]
  [InlineData("ID lt 30", "WHERE [t_MonkeyTable].[Id] < @V0", "30", "30", 1)]
  [InlineData("ID le 30", "WHERE [t_MonkeyTable].[Id] <= @V0", "30", "30", 1)]
  [InlineData("NaMe in ('Bill','Susan','Jack')", "WHERE [t_MonkeyTable].[Name] IN (@V0, @V1, @V2)", "Bill", "Jack", 3)]
  [InlineData("NamE ct 'Bill'", "WHERE [t_MonkeyTable].[Name] LIKE @V0", "%Bill%", "%Bill%", 1)]
  [InlineData("Name sw 'Bill'", "WHERE [t_MonkeyTable].[Name] LIKE @V0", "Bill%", "Bill%", 1)]
  public void ConvertToSqlWhereClause_HandlesCommonCases(string filterString, string expected, string first, string last, int count)
  {
    // Arrange
    MsSqlDialectBuilder sut = new MsSqlDialectBuilder(TestSampleData.DB);

    // Act
    var actual = sut.BuildWhereClause(filterString, sut.FindEntity("dbo", "MonkeyTable"));

    // Assert
    Assert.Equal(expected, actual.Sql);
    Assert.Equal(first, actual.Parameters.First().Value.ToString());
    Assert.Equal(last, actual.Parameters.Last().Value.ToString());
    Assert.Equal(count, actual.Parameters.Count);
  }

  // Filters that contain dates and times
  [InlineData("skillKey eq 'cdl' AND effectiveStartDate le '2023-11-11T22:00:00-0400' AND effectiveEndDate gt '2023-11-12T06:00:00-0400'",
              "WHERE [t_MonkeyTable].[skillKey] = @V0 AND [t_MonkeyTable].[effectiveStartDate] <= @V1 AND [t_MonkeyTable].[effectiveEndDate] > @V2",
              "cdl", "2023-11-12T10:00:00.0000000Z", 3)]
  public void ConvertToSqlWhereClause_HandlesDateTimeCases(string filterString, string expected, string first, string last, int count)
  {
    // Arrange
    MsSqlDialectBuilder sut = new MsSqlDialectBuilder(TestSampleData.DB);

    // Act
    var actual = sut.BuildWhereClause(filterString, sut.FindEntity("dbo", "MonkeyTable"));

    // Assert
    Assert.Equal(expected, actual.Sql);
    Assert.Equal(first, actual.Parameters.First().Value.ToString());
    var l = (DateTimeOffset)actual.Parameters.Last().Value;
    Assert.Equal(last, l.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"));
    Assert.Equal(count, actual.Parameters.Count);
  }

  [Theory]
  [InlineData("", "No input string")]
  [InlineData(" ", "No input string")]
  [InlineData("        ", "No input string")]
  [InlineData(" \t\n     ", "No input string")]
  [InlineData("((( ID eq 42", "Invalid parens")]
  [InlineData("eq 42", "No column given")]
  [InlineData("ID eq 42 AND", "AND but no second condition")]
  [InlineData(",", "No Tokens in the input string")]
  [InlineData("('1' eq ID)", "Filter conditions MUST the format `property operator value` to be valid")]
  [InlineData("FooBar eq 'nope'", "column FooBar does not exist on this table.")]
  public void ConvertToSqlWhereClause_ReturnsEmptyString_WhenInputFilterStringIsNull(string input, string error)
  {
    // Arrange
    string filterString = input;
    MsSqlDialectBuilder sut = new MsSqlDialectBuilder(TestSampleData.DB);

    // Act
    ArgumentException ex = Assert.ThrowsAny<ArgumentException>(() => sut.BuildWhereClause(filterString, sut.FindEntity("dbo", "MonkeyTable")));

    if (ex == null)
      throw new Exception(error);
  }
}
