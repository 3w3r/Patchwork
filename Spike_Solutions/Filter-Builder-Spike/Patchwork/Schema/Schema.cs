using System.Collections.ObjectModel;

namespace Patchwork.Schema;

public record Schema(string Name,
                     ReadOnlyCollection<Table> Tables,
                     ReadOnlyCollection<View> Views);
