using System.Collections.ObjectModel;

namespace Patchwork.DbSchema;

public record Schema(string Name,
                     ReadOnlyCollection<Table> Tables,
                     ReadOnlyCollection<View> Views);
