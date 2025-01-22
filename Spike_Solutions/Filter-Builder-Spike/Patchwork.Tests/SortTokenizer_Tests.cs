using System.Collections.ObjectModel;
using Patchwork.Sort;

namespace Patchwork.Tests;

public class SortTokenizer_Tests
{
  [Fact]
  public void Parse_ReturnsEmptyString_WhenNoExpressionProvided()
  {
    // Arrange
    var tokens = new List<SortToken>(); // No tokens provided
    var parser = new MsSortTokenParser(tokens);

    // Act
    var result = parser.Parse();

    // Assert
    Assert.Empty(tokens);
  }

  [Theory]
  [InlineData("firstname:asc,lastname:desc", "[firstname], [lastname] DESC", 2, 1, 1)]
  [InlineData("firstname:asc       ,lastname    :desc       ", "[firstname], [lastname] DESC", 2, 1, 1)]
  [InlineData("       firstname:asc,lastname         :desc", "[firstname], [lastname] DESC", 2, 1, 1)]
  [InlineData("A,B,C,D,E,F", "[A], [B], [C], [D], [E], [F]", 6, 6, 0)]
  [InlineData("A,B:desc,C,D:desc,E,F", "[A], [B] DESC, [C], [D] DESC, [E], [F]", 6, 4, 2)]
  [InlineData("A,B:asc,C,D:asc,E:desc,F", "[A], [B], [C], [D], [E] DESC, [F]", 6, 5, 1)]
  [InlineData("A,But_I_Think_This_Column_Name_Is_Really_Long:desc", "[A], [But_I_Think_This_Column_Name_Is_Really_Long] DESC", 2, 1, 1)]
  public void Parse_ReturnsOrderByClause_MsSqlSuccessCases(string sort, string expected, int count, int ascending, int descending)
  {
    // Arrange
    var lex = new SortLexer(sort);

    // Act
    var tokens = lex.Tokenize();
    var parser = new MsSortTokenParser(tokens);
    var result = parser.Parse();

    // Assert
    Assert.Equal(count, tokens.Count);
    Assert.Equal(expected, result);
    Assert.Equal(ascending, tokens.Where(t => t.Direction == SortDirection.Ascending).Count());
    Assert.Equal(descending, tokens.Where(t => t.Direction == SortDirection.Descending).Count());
  }

  [Theory]
  [InlineData("firstName:asc,lastName:desc", "firstname, lastname desc", 2, 1, 1)]
  [InlineData("firstName:asc       ,lastname    :desc       ", "firstname, lastname desc", 2, 1, 1)]
  [InlineData("       firstname:asc,lastname         :desc", "firstname, lastname desc", 2, 1, 1)]
  [InlineData("A,B,C,D,E,F", "a, b, c, d, e, f", 6, 6, 0)]
  [InlineData("A,B:desc,C,D:desc,E,F", "a, b desc, c, d desc, e, f", 6, 4, 2)]
  [InlineData("A,B:asc,C,D:asc,E:desc,F", "a, b, c, d, e desc, f", 6, 5, 1)]
  [InlineData("A,But_I_Think_This_Column_Name_Is_Really_Long:desc", "a, but_i_think_this_column_name_is_really_long desc", 2, 1, 1)]
  public void Parse_ReturnsOrderByClause_PostgreSqlSuccessCases(string sort, string expected, int count, int ascending, int descending)
  {
    // Arrange
    var lex = new SortLexer(sort);

    // Act
    var tokens = lex.Tokenize();
    var parser = new PostgreSqlSortTokenParser(tokens);
    var result = parser.Parse();

    // Assert
    Assert.Equal(count, tokens.Count);
    Assert.Equal(expected, result);
    Assert.Equal(ascending, tokens.Where(t => t.Direction == SortDirection.Ascending).Count());
    Assert.Equal(descending, tokens.Where(t => t.Direction == SortDirection.Descending).Count());
  }

  [Theory]
  [InlineData("firstname:asc,la%name:desc", "Invalid sort expression: la%name:desc is not a valid field name")]
  [InlineData("^A,B,C,D,E,F:desc", "Invalid sort expression: ^A is not a valid field name")]
  [InlineData("A,B,C,D,E,F:dasc", "Invalid sort expression: dasc is not valid sort order")]
  [InlineData("A,B,C,D,E,F:goat roper", "Invalid sort expression: 'goat roper' is not valid sort order")]
  [InlineData("A B C", "Invalid sort expression: Invalid column names")]
  [InlineData("A,Bad;character", "Invalid sort expression: Invalid column names")]
  public void Parse_ThrowsArgumentException_ForInvalidSortExpression(string sort, string errorMessage)
  {
    // Arrange
    var lex = new SortLexer(sort);

    var ex = Assert.ThrowsAny<ArgumentException>(() =>
    {
      var tokens = lex.Tokenize();
      var parser = new MsSortTokenParser(tokens);
      var result = parser.Parse();
    });
    if (ex == null) throw new Exception(errorMessage);
  }
}
