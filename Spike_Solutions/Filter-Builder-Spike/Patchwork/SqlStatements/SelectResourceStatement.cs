namespace Patchwork.SqlStatements;

// PRIMARY SQL COMMANDS
public record class SelectListStatement(string Sql, string CountSql, Dictionary<string, object> Parameters);
public record class SelectResourceStatement(string Sql, Dictionary<string, object> Parameters);
public record class InsertStatement(string Sql, Dictionary<string, object> Parameters);
public record class UpdateStatement(string Sql, Dictionary<string, object> Parameters);
public record class DeleteStatement(string Sql, Dictionary<string, object> Parameters);
public record class PatchStatement(string Sql, Dictionary<string, object> Parameters);

// SECONDARY SQL COMMANDS
public record class FilterStatement(string Sql, Dictionary<string, object> Parameters);
