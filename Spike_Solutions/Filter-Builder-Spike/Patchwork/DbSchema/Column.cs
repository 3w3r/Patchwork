namespace Patchwork.DbSchema;

public record Column(string Name, string Description, Type DataFormat, bool IsPrimaryKey, bool IsForeignKey, string ForeignKeyTableName, bool IsAutoNumber, bool IsComputed, bool IsUniqueKey, bool IsIndexed);