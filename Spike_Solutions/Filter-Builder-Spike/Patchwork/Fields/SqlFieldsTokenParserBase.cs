namespace Patchwork.Fields;

public abstract class SqlFieldsTokenParserBase
{
  protected readonly List<FieldsToken> _tokens;

  public SqlFieldsTokenParserBase(List<FieldsToken> tokens)
  {
    _tokens = tokens;
  }

  public abstract string Parse();
}
