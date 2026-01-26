using System.Data.Common;
using GalacticDelivery.Application;
using Microsoft.Data.Sqlite;

namespace GalacticDelivery.Infrastructure;

public sealed class SqliteTransactionManager : ITransactionManager
{
    private readonly SqliteConnection _connection;

    public SqliteTransactionManager(SqliteConnection connection)
    {
        _connection = connection;
    }

    public ValueTask<DbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return _connection.BeginTransactionAsync(cancellationToken);
    }
}
