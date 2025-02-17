namespace Patchwork.DbSchema;

public record DatabaseMetadata(IList<Schema> Schemas, bool HasPatchTracking);
