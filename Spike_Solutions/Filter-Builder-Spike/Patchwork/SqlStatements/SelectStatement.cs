namespace Patchwork.SqlStatements;

public record class SelectStatement(string Sql, Dictionary<string,object> Parameters);
public record class FilterStatement(string Sql, Dictionary<string,object> Parameters);
