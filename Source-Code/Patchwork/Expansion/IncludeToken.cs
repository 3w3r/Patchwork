namespace Patchwork.Expansion;

public record IncludeToken(string ChildSchemaName, string ChildTableName, string ChildTablePkName, string ParentSchemaName, string ParentTableName, string ParentTableFkName);
