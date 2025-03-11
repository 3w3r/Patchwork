namespace Patchwork.SqlStatements;

public class PatchworkLogEvent
{
  public long Pk { get; set; }
  public DateTimeOffset EventDate { get; set; }
  public HttpMethodsEnum HttpMethod { get; set; } = HttpMethodsEnum.Unknown;
  public string Domain { get; set; } = string.Empty;
  public string Entity { get; set; } = string.Empty;
  public string Id { get; set; } = string.Empty;
  public LogStatusEnum Status { get; set; } = LogStatusEnum.Unknown;
  public string Patch { get; set; } = "[]";
}

public enum HttpMethodsEnum
{
  Unknown = 0,
  Patch = 1,
  Post = 2,
  Put = 3,
  Delete = 4
}

[Flags]
public enum LogStatusEnum
{
  Unknown = 0,
}
