namespace Patchwork.SqlStatements;

// PRIMARY SQL COMMANDS
public record SelectListStatement(string Sql, string CountSql, Dictionary<string, object> Parameters);
public record SelectResourceStatement(string Sql, Dictionary<string, object> Parameters);
public record SelectEventLogStatement(string Sql, Dictionary<string, object> Parameters);
public record InsertStatement(string Sql, Dictionary<string, object> Parameters);
public record UpdateStatement(string Sql, Dictionary<string, object> Parameters);
public record DeleteStatement(string Sql, Dictionary<string, object> Parameters);
public record PatchStatement(string Sql, Dictionary<string, object> Parameters);

// SECONDARY SQL COMMANDS
public record FilterStatement(string Sql, Dictionary<string, object> Parameters);
public record PatchworkLogEvent(long Pk, DateTimeOffset EventDate, string Domain, string Entity, string Id, string Patch);
