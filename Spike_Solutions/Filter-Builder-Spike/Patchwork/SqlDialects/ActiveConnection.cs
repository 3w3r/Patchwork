using System.Data.Common;

namespace Patchwork.SqlDialects;

public class ActiveConnection : IDisposable
{
  public DbConnection Connection { get; init; }
  public DbTransaction Transaction { get; init; }

  public ActiveConnection(DbConnection connection, DbTransaction transaction)
  {
    Connection = connection; 
    Transaction = transaction;
  }

  public void Dispose()
  {
    Transaction.Dispose();
    if(Connection.State == System.Data.ConnectionState.Open) Connection.Close();
    Connection.Dispose();
  }
}
