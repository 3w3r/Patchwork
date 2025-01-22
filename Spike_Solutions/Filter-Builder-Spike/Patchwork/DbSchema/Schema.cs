using System.Collections.ObjectModel;

namespace Patchwork.DbSchema;

public record Schema(string Name,
                     IList<Entity> Tables,
                     IList<Entity> Views);
