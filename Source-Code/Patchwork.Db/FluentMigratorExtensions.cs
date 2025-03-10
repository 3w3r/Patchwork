using System.Reflection;
using FluentMigrator;
using FluentMigrator.Builders.Create.Table;
using FluentMigrator.Builders.Insert;
using FluentMigrator.Builders.Update;

namespace Patchwork.Db;

public static class FluentMigratorExtensions
{
  public static IUpdateSetSyntax InSchemaIfSupported(this IUpdateSetOrInSchemaSyntax builder)
  {
    if (MigrationConfigurations.DbType == DbTypeEnum.Sqlite)
    {
      return builder;
    }
    else
    {
      return builder.InSchema("surveys");
    }
  }

  public static ICreateTableWithColumnSyntax InSchemaIfSupported(this ICreateTableWithColumnOrSchemaOrDescriptionSyntax builder)
  {
    if (MigrationConfigurations.DbType == DbTypeEnum.Sqlite)
    {
      return builder;
    }
    else
    {
      return builder.InSchema("surveys");
    }
  }

  public static ICreateTableColumnOptionOrWithColumnSyntax AsSequentialGuid(this ICreateTableColumnAsTypeSyntax builder)
  {
    if (MigrationConfigurations.DbType == DbTypeEnum.Sqlite)
    {
      return builder.AsGuid();
    }
    else
    {
      return builder.AsGuid().WithDefault(SystemMethods.NewGuid);
    }
  }

  public static IInsertDataSyntax InSchemaIfSupported(this IInsertDataOrInSchemaSyntax builder)
  {
    if (MigrationConfigurations.DbType == DbTypeEnum.Sqlite)
    {
      return builder;
    }
    else
    {
      return builder.InSchema("surveys");
    }
  }

  public static ICreateTableColumnOptionOrForeignKeyCascadeOrWithColumnSyntax AsForeignKey(this ICreateTableColumnOptionOrWithColumnSyntax builder, string foreignKeyName, string schemaName, string primaryTableName, string primaryTableKey)
  {
    if (MigrationConfigurations.DbType == DbTypeEnum.Sqlite)
    {
      return builder.ForeignKey(primaryTableName, primaryTableKey);
    }
    else
    {
      return builder.ForeignKey(foreignKeyName, schemaName, primaryTableName, primaryTableKey);
    }
  }

  public static string ReadEmbeddedTextFile(this Migration m, string resourceName)
  {
    // Guard
    if (string.IsNullOrEmpty(resourceName))
      return string.Empty;

    // Get the current assembly and resource name
    Assembly assembly = Assembly.GetExecutingAssembly();
    string[] resourceNames = assembly.GetManifestResourceNames();
    IEnumerable<string> found = resourceNames.Where(x => x.Contains(resourceName));
    if (found.Count() != 1)
      throw new ArgumentException($"Resource {resourceName} not found or ambiguous.");

    // Access the embedded resource stream
    using Stream stream = assembly.GetManifestResourceStream(resourceName) ?? throw new FileNotFoundException($"Resource {resourceName} not found.");

    // Read the content
    using StreamReader reader = new StreamReader(stream);
    return reader.ReadToEnd();
  }
}
