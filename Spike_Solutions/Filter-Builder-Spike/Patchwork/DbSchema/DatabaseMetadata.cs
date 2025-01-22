using System.Collections.ObjectModel;

namespace Patchwork.DbSchema;

public record DatabaseMetadata(IList<Schema> Schemas);
