namespace Patchwork.Paging;

public class PagingToken
{
  public int Limit { get; private set; }
  public int Offset { get; private set; }

  public PagingToken(int limit = 25, int offset = 0)
  {
    if (limit > 0)
    {
      if (limit <= 5000)
      {
        Limit = limit; // Value between 0 and 5000 so use it
      }
      else
      {
        Limit = 5000; // Default limit to 5000 if greater than 5000
      }
    }
    else
    {
      Limit = 25; // Default limit to 25 if not provided or less than zero
    }

    if (offset >= 0)
    {
      Offset = offset; // Offset is non-negative, so use it
    }
    else
    {
      Offset = 0; // Default offset to 0 if not provided
    }
  }
}
