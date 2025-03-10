using System.Data.Common;

namespace Patchwork.SqlDialects;

public class ReaderConnection : IDisposable
{
  public DbConnection Connection { get; init; }

  public ReaderConnection(DbConnection connection)
  {
    Connection = connection;
  }
  public void Dispose()
  {
    if (Connection.State == System.Data.ConnectionState.Open)
      Connection.Close();
    Connection.Dispose();
  }
}
