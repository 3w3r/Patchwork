namespace Patchwork.Expansion;

public abstract class IncludeTokenParserBase
{
  protected List<IncludeToken> _tokens;
  public IncludeTokenParserBase(List<IncludeToken> tokens)
  {
    _tokens = tokens;
  }

  public abstract string Parse();
}
