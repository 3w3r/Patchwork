namespace Patchwork.Sort;

public record SortToken(string EntityName, string Column, SortDirection Direction = SortDirection.Ascending);
