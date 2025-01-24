using Patchwork.Sort;
using Patchwork.SqlDialects;

namespace Patchwork.Tests;

public class SortTokenizer_Tests
{
  [Fact]
  public void Parse_ReturnsEmptyString_WhenNoExpressionProvided()
  {
    // Arrange
    List<SortToken> tokens = new List<SortToken>(); // No tokens provided
    MsSortTokenParser parser = new MsSortTokenParser(tokens);

    // Act
    string result = parser.Parse();

    // Assert
    Assert.Empty(tokens);
  }

  [Theory]
  [InlineData("firstname:asc,lastname:desc", "ORDER BY [firstname], [lastname] DESC, [A]")]
  [InlineData("firstname:asc       ,lastname    :desc       ", "ORDER BY [firstname], [lastname] DESC, [A]")]
  [InlineData("       firstname:asc,lastname         :desc", "ORDER BY [firstname], [lastname] DESC, [A]")]
  [InlineData("A,B,C,D,E,F", "ORDER BY [A], [B], [C], [D], [E], [F]")]
  [InlineData("A,B:desc,C,D:desc,E,F", "ORDER BY [A], [B] DESC, [C], [D] DESC, [E], [F]")]
  [InlineData("A,B:asc,C,D:asc,E:desc,F", "ORDER BY [A], [B], [C], [D], [E] DESC, [F]")]
  [InlineData("A,But_I_Think_This_Column_Name_Is_Really_Long:desc", "ORDER BY [A], [But_I_Think_This_Column_Name_Is_Really_Long] DESC")]
  public void Parse_ReturnsOrderByClause_MsSqlSuccessCases(string sort, string expected)
  {
    // Arrange

    MsSqlDialectBuilder sut = new MsSqlDialectBuilder(TestSampleData.DB);

    // Act
    string result = sut.BuildOrderByClause(sort, "A", "SortTest");

    // Assert
    Assert.Equal(expected, result);
  }

  [Theory]
  [InlineData("firstName:asc,lastName:desc", "ORDER BY firstname, lastname desc, a")]
  [InlineData("firstName:asc       ,lastname    :desc       ", "ORDER BY firstname, lastname desc, a")]
  [InlineData("       firstname:asc,lastname         :desc", "ORDER BY firstname, lastname desc, a")]
  [InlineData("A,B,C,D,E,F", "ORDER BY a, b, c, d, e, f")]
  [InlineData("A,B:desc,C,D:desc,E,F", "ORDER BY a, b desc, c, d desc, e, f")]
  [InlineData("A,B:asc,C,D:asc,E:desc,F", "ORDER BY a, b, c, d, e desc, f")]
  [InlineData("A,But_I_Think_This_Column_Name_Is_Really_Long:desc", "ORDER BY a, but_i_think_this_column_name_is_really_long desc")]
  public void Parse_ReturnsOrderByClause_PostgreSqlSuccessCases(string sort, string expected)
  {

    // Arrange
    PostgreSqlDialectBuilder sut = new PostgreSqlDialectBuilder(TestSampleData.DB);

    // Act
    string result = sut.BuildOrderByClause(sort, "A", "SortTest");

    // Assert
    Assert.Equal(expected, result);
  }

  [Theory]
  [InlineData("firstname:asc,la%name:desc", "Invalid sort expression: la%name:desc is not a valid field name")]
  [InlineData("OD", "Invalid sort expression: OD is not a column in the table")]
  [InlineData("^A,B,C,D,E,F:desc", "Invalid sort expression: ^A is not a valid field name")]
  [InlineData("A,B,C,D,E,F:dasc", "Invalid sort expression: dasc is not valid sort order")]
  [InlineData("A,B,C,D,E,F:goat roper", "Invalid sort expression: 'goat roper' is not valid sort order")]
  [InlineData("A B C", "Invalid sort expression: Invalid column names")]
  [InlineData("A,Bad;character", "Invalid sort expression: Invalid column names")]
  public void Parse_ThrowsArgumentException_ForInvalidSortExpression(string sort, string errorMessage)
  {

    MsSqlDialectBuilder sut = new MsSqlDialectBuilder(TestSampleData.DB);

    ArgumentException ex = Assert.ThrowsAny<ArgumentException>(() =>
    {
      string result = sut.BuildOrderByClause(sort, "A", "SortTest");

    });
    if (ex == null)
      throw new Exception(errorMessage);
  }
}
