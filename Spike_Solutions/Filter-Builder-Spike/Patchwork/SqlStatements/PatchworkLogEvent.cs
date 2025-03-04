namespace Patchwork.SqlStatements;

public class PatchworkLogEvent
{
  public long Pk { get; set; }
  public DateTimeOffset EventDate { get; set; }
  public string Domain { get; set; } = string.Empty;
  public string Entity { get; set; } = string.Empty;
  public string Id { get; set; } = string.Empty;
  public string Patch { get; set; } = "[]";
}
