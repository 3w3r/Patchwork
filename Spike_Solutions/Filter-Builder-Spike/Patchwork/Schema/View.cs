using System.Collections.ObjectModel;

namespace Patchwork.Schema;

public record View(string Name, string Description, ReadOnlyCollection<Column> Columns);
