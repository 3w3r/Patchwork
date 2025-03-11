using System.Data.Common;

namespace Patchwork.SqlDialects;

public class WriterConnection : IDisposable
{
  public DbConnection Connection { get; init; }
  public DbTransaction Transaction { get; init; }

  public WriterConnection(DbConnection connection, DbTransaction transaction)
  {
    Connection = connection;
    Transaction = transaction;
  }

  /// <summary>
  /// Disposes of the <see cref="WriterConnection"/> and its associated resources.
  /// </summary>
  public void Dispose()
  {
    // Dispose of the transaction
    Transaction.Dispose();

    // If the connection is open, close it
    if (Connection.State == System.Data.ConnectionState.Open)
      Connection.Close();

    // Dispose of the connection
    Connection.Dispose();
  }
}
