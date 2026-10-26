[![NuGet - QChain](https://img.shields.io/nuget/vpre/QChain?label=QChain)](https://www.nuget.org/packages/QChain)
[![Tests](https://github.com/MihaiBratulescu/QChain/actions/workflows/tests.yml/badge.svg)](https://github.com/MihaiBratulescu/QChain/actions/workflows/tests.yml)
[![License](https://img.shields.io/github/license/MihaiBratulescu/QChain?label=License)](https://github.com/MihaiBratulescu/QChain/blob/master/LICENSE)

---

# QChain

**Composable LINQ query pipelines with named tuples.**

QChain lets you return joined queries from methods and keep composing them through `IQueryable<T>`. Define a shared pipeline once, then add filters, sorting, pagination, and projections where you use it.

## Motivation

Sharing a joined LINQ query across methods often means declaring a class or record just to hold the intermediate results.

QChain lets you use named tuples instead. Return a joined pipeline as `IQueryable<(Customer customer, Order order)>`, then build on it with familiar LINQ operators.

For reusable predicates and expression composition, QChain can be paired with a library such as NeinLinq or LINQKit.

---

## &#x1F44E; Before QChain

Large EF Core applications often end up with long methods, and duplicated, tightly coupled query logic.

- Joins produce anonymous intermediate types, making query composition and reuse difficult.
- Mapping is either baked in or deferred until after execution, requiring full entities to be loaded.
- Pagination, sorting, or extra filters require more repository methods and a wider API surface.

```csharp
public Task<List<CustomerBalanceDto>> GetActiveEuropeanCustomerPaymentsAsync(CancellationToken ct)
{
    return db.Customers
        .Where(c => c.IsActive && c.Region == "EU")                                     // duplicated predicates
        .Join(db.Orders, c => c.Id, o => o.CustomerId, (c, o) => new { c, o })          // anonymous<Customer, Order>
        .Join(db.Payments, x => x.o.Id, p => p.OrderId, (x, p) => new { x.c, x.o, p })  // anonymous<Customer, Order, Payment>
        .Select(x => new CustomerBalanceDto(x.c.Id, x.c.Name, x.p.Amount))  // mapping baked into DAL layer
        .ToArrayAsync(ct);                                                  // no pagination support
}

public Task<List<CustomerRiskDto>> GetRecentEuropeanCustomerRisksAsync(decimal threshold, CancellationToken ct)
{
    return db.Customers
        .Where(c => c.IsActive && c.Region == "EU")                                     // duplicated predicates
        .Join(db.Orders, c => c.Id, o => o.CustomerId, (c, o) => new { c, o })          // anonymous<Customer, Order>
        .Join(db.Payments, x => x.o.Id, p => p.OrderId, (x, p) => new { x.c, x.o, p })  // anonymous<Customer, Order, Payment>
        .Where(x => x.p.Amount >= threshold)
        .Select(x => new CustomerRiskDto(x.c.Id, x.c.Name, risk: "High"))  // mapping baked into DAL layer
        .ToArrayAsync(ct);                                                 // no pagination support
}
```

## &#x1F44D; With QChain

Readable, reusable, and aligned with your domain. QChain keeps intermediate query shapes as named tuples instead of anonymous types.

```csharp
public IQueryable<(Customer c, Order o, Payment p)> GetActiveEuropeanCustomerPayments()
{
    return CustomerPayments()       // reusable joined query shape
        .Where(x => x.c.IsActive()  // composable predicates
            .And(x.c.FromEurope()));
}

public IQueryable<(Customer c, Order o, Payment p)> GetRecentEuropeanCustomerRisks(decimal threshold)
{
    return GetActiveEuropeanCustomerPayments()  // Tuple<(Customer c, Order o, Payment p)>
        .Where(x => x.p.Amount >= threshold));
}

private IQueryable<(Customer c, Order o, Payment p)> CustomerPayments()
{
    return db.Customers
        .Join(db.Orders, c => c.Id, o => o.CustomerId,
            (c, o) => new ValueTuple<Customer, Order>(c, o))
        .Join(db.Payments, x => x.Item2.Id, p => p.OrderId,
            (x, p) => new ValueTuple<Customer, Order, Payment>(x.Item1, x.Item2, p));
}
```

## &#x1F517; Composing at the Call Site

Mapping and pagination compose externally. Query composition is reusable while execution concerns remain composable.

```csharp
var balances = await unitOfWork.Query(db => db.Customers
        .GetActiveEuropeanCustomerBalances()
        .Select(x => new CustomerBalanceDto(x.c.Id, x.c.Name, x.p.Amount))  // mapping remains at the calling layer
        .Page(index, size))                                                 // pagination is applied as a query extension 
    .ToArrayAsync(ct);

var risks = await unitOfWork.Query(db => db.Customers
        .GetRecentEuropeanCustomerRisks(threshold: 10_000)
        .Select(x => new CustomerRiskDto(x.c.Id, x.c.Name, risk: "High"))  // mapping remains at the calling layer
        .Page(index, size))                                                // pagination is applied as a query extension 
    .ToArrayAsync(ct);
```

---

## &#x1F3D7;&#xFE0F; Basic Usage

Start from any `IQueryable<T>` and wrap it in a `Query<T>`.

```csharp
IQueryable<Account> Accounts = new Query<Account>(db.Set<Account>());
IQueryable<Order> Orders = new Query<Order>(db.Set<Order>());
```

Define reusable predicates as extension methods over the entity type. 

```csharp
public static class AccountPredicates
{
    public static Expression<Func<Account, bool>> IsActive(this Account _)
        => account => account.IsActive;

    public static Expression<Func<Account, bool>> FromEurope(this Account _)
        => account => account.Region == Region.Europe;
}

public static class OrderPredicates
{
    public static Expression<Func<Order, bool>> InLastMonth(this Order _)
        => order => order.CreatedDate >= DateTime.UtcNow.AddMonths(-1);

    public static Expression<Func<Order, bool>> InEuro(this Order _)
        => order => order.CurrencyId == CurrencyType.EUR;
}
```

## Unit of Work

Expose query roots from your unit of work as `IQueryable<T>` and provide a small set of `Query` overloads for invoking query logic inside that unit of work boundary. `QueryExecutor<T>` is provided for deferred execution.

```csharp
public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public IQueryable<Account> Accounts { get; } = new Query<Account>(db.Set<Account>());
    public IQueryable<Order> Orders { get; } = new Query<Order>(db.Set<Order>());

    public IAccountsRepository AccountsRepository { get; } = new AccountsRepository(this);

    public T Query<T>(Func<IUnitOfWork, T> query) => query(this);

    public Task<T> Query<T>(Func<IUnitOfWork, Task<T>> query) => query(this);

    public IQueryExecutor<T> Query<T>(Func<IUnitOfWork, IQueryable<T>> query) =>
        new QueryExecutor<T>(query(this));
}
```

- `Query<T>(Func<IUnitOfWork, T>)` for immediate values
- `Query<T>(Func<IUnitOfWork, Task<T>>)` for async work
- `Query<T>(Func<IUnitOfWork, IQueryable<T>>)` for deferred query execution

```csharp
int totalAccounts = unitOfWork.Query(db => db.Accounts.Count());

int activeCount = await unitOfWork.Query(db => db.Accounts
        .Where(a => a.IsActive())
        .CountAsync());

(Account account, Order order)[] ordersInLastMonth = await unitOfWork.Query(db =>
        db.AccountsRepository.ActiveEuropeanOrdersInLastMonth())
    .ToArrayAsync();
```

```csharp
public class AccountsRepository(IUnitOfWork db)
{
    public IQueryable<(Account account, Order order)> ActiveEuropeanOrdersInLastMonth() =>
        db.Accounts
            .Join(db.Orders, a => a.AccountId, o => o.AccountId,
                (a, o) => ValueTuple.Create(a, o))
            // predicates are reusable across joins
            .Where(x => x.account.IsActive().And(x.order.InLastMonth())); 
}
```

---

## Package and Dependencies

```bash
dotnet add package QChain
```

QChain supports EF Core 8, 9, and 10.

> **Note:** `DefaultIfEmpty()` on nullable tuple projections is not supported with EF Core 8 or 9. Use EF Core 10 for these queries.

---

## Feedback/Contribution

[Issues](https://github.com/MihaiBratulescu/QChain/issues), [Discussions](https://github.com/MihaiBratulescu/QChain/discussions), and [Pull Requests](https://github.com/MihaiBratulescu/QChain/pulls) are welcome.
