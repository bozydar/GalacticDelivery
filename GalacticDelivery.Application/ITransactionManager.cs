using System.Data;
using System.Data.Common;

namespace GalacticDelivery.Application;

public interface ITransactionManager
{
    ValueTask<DbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
