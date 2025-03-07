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

  /// <summary>
  /// Parses the limit value for the paging token.
  /// </summary>
  /// <param name="limit">The limit value to be parsed.</param>
  /// <returns>The parsed limit value.</returns>
  public static int ParseLimit(int limit)
  {
    // Check if the limit is greater than zero
    if (limit > 0)
    {
      // Check if the limit is less than or equal to the maximum record limit
      if (limit <= PatchworkConfig.MaxRecordLimit)
      {
        // If the limit is within the valid range, return the limit value
        return limit;
      }
      else
      {
        // If the limit is greater than the maximum record limit, return the maximum record limit
        return PatchworkConfig.MaxRecordLimit;
      }
    }
    else
    {
      // If the limit is not provided or less than zero, return the minimum record limit
      return PatchworkConfig.MinRecordLimit;
    }
  }
}
