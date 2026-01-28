using System.Data;
using System.Data.Common;
using GalacticDelivery.Common;

namespace GalacticDelivery.Application;

public interface ITransactionManager
{
    ValueTask<DbTransaction> BeginTransactionAsync();
}

public static class TransactionManagerExtensions
{
    public static async Task<Result<T>> WithTransaction<T>(this ITransactionManager transactionManager, Func<DbTransaction, Task<Result<T>>> body)
    {
        await using var transaction = await transactionManager.BeginTransactionAsync();
        try
        {
            var result = await body(transaction);
            if (result.IsFailure)
            {
                await transaction.RollbackAsync();
            }
            else
            {
                await transaction.CommitAsync();
            }

            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
