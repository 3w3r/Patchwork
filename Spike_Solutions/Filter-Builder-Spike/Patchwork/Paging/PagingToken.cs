namespace Patchwork.Paging;

public class PagingToken
{
  public int Limit { get; private set; }
  public long Offset { get; private set; }

  public PagingToken(int limit, long offset)
  {
    Limit = ParseLimit(limit);

    if (offset >= 0)
    {
      Offset = offset; // Offset is non-negative, so use it
    }
    else
    {
      Offset = 0; // Default offset to 0 if not provided
    }
  }
  public static int ParseLimit(int limit)
  {
    if (limit > 0)
    {
      if (limit <= PatchworkConfig.MaxRecordLimit)
      {
        return limit; // Value between 0 and 5000 so use it
      }
      else
      {
        return PatchworkConfig.MaxRecordLimit; // Default limit to 5000 if greater than 5000
      }
    }
    else
    {
      return PatchworkConfig.MinRecordLimit; // Default limit to 25 if not provided or less than zero
    }
  }
}
