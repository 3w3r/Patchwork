namespace Patchwork.Expansion;

public record IncludeToken(string ChildTableName, string ChildTablePkName, string ParentTableName, string ParentTableFkName)
{
  public string ChildTablePrefixName => $"T_{ChildTableName}";
  public string ParentTablePrefixName => $"T_{ParentTableName}";
};
