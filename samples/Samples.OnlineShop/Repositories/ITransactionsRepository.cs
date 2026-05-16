using QChain;
using QChain.EntityFrameworkCore;
using Samples.OnlineShop.DatabaseModels;

namespace Samples.OnlineShop.Repositories;

public interface ITransactionsRepository : IQuery<Transaction>
{
    ITransactionsRepository Settled();
}

public class TransactionsRepository(IQueryable<Transaction> query) : EntityQuery<Transaction>(query), ITransactionsRepository
{
    private TransactionsRepository(IQuery<Transaction> query) : this(query.AsQueryable())
    {
    }

    public ITransactionsRepository Settled() =>
        new TransactionsRepository(Where(t => t.Status == TransactionStatus.Settled));
}
