using System.Collections.ObjectModel;

namespace Patchwork.DbSchema;

public record DatabaseMetadata(ReadOnlyCollection<Schema> Schemas);
