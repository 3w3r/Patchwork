using Patchwork.Filters;

namespace Patchwork.Tests
{
  public class TestTokenTypes
  {
    [Fact]
    public void AllTokenTypes_Are_Defined()
    {
      FilterTokenType t = FilterTokenType.Textual;
      Assert.True(FilterTokenType.Value.HasFlag(t));
    }
  }
}
