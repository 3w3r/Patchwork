using System.Collections.ObjectModel;

namespace Patchwork.Schema;

public record DatabaseMetadata(ReadOnlyCollection<Schema> Schemas);
