﻿using System.Text;
using Patchwork.Expansion;

namespace Patchwork.SqlDialects.Sqlite;

public class SqliteIncludeTokenParser : IncludeTokenParserBase
{
  public SqliteIncludeTokenParser(List<IncludeToken> tokens) : base(tokens) { }

  public override string Parse()
  {
    StringBuilder sb = new StringBuilder();
    foreach (IncludeToken token in _tokens)
    {
      string cs = string.IsNullOrEmpty(token.ChildSchemaName) ? "" : $"{token.ChildSchemaName}.";

      sb.AppendLine($"LEFT OUTER JOIN {cs}{token.ChildTableName} AS t_{token.ChildTableName} ON " +
                    $"t_{token.ParentTableName}.{token.ParentTableFkName} = " +
                    $"t_{token.ChildTableName}.{token.ChildTablePkName}");
    }
    return sb.ToString();
  }
}
