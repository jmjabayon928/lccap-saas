using Lccap.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Lccap.Infrastructure.Persistence.Transactions;

public sealed class EfCoreLccapDbTransaction : ILccapDbTransaction
{
    private readonly IDbContextTransaction _transaction;

    public EfCoreLccapDbTransaction(IDbContextTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        _transaction = transaction;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        _transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default) =>
        _transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}

