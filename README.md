[![NuGet - QChain](https://img.shields.io/nuget/vpre/QChain?label=QChain)](https://www.nuget.org/packages/QChain)
[![NuGet - QChain.EntityFrameworkCore](https://img.shields.io/nuget/vpre/QChain.EntityFrameworkCore?label=QChain.EntityFrameworkCore)](https://www.nuget.org/packages/QChain.EntityFrameworkCore)
[![Tests](https://github.com/MihaiBratulescu/QChain/actions/workflows/tests.yml/badge.svg)](https://github.com/MihaiBratulescu/QChain/actions/workflows/tests.yml)
[![License](https://img.shields.io/github/license/MihaiBratulescu/QChain?label=License)](https://github.com/MihaiBratulescu/QChain/blob/master/LICENSE)

---

# QChain

**LINQ specification pattern for building reusable and composable query pipelines.**

QChain lets you build reusable, composable, and expressive query pipelines on top of LINQ.  
Instead of duplicating query logic across repositories and services, you define query fragments once and chain them together.

---

## &#x2728; Motivation

LINQ is powerful, but in real-world applications it often leads to:
- duplicated query logic
- hard-to-read query chains
- bloated repositories
- poor support for reusable specifications

### What QChain Solves
- Reusable predicates
- Composable pipelines
- Flexible construction and execution

---

## &#x1F44E; Before QChain

Large EF Core applications often end up with long methods, and duplicated, tightly coupled query logic.

- Joins produce anonymous intermediate types, making query composition and reuse difficult.
- Mapping is either baked in or deferred until after execution, requiring full entities to be loaded.
- Pagination, sorting, or extra filters require more repository methods and a wider API surface.

```csharp
public Task<List<CustomerBalanceDto>> GetActiveEuropeanCustomerBalancesAsync(DateTime from, CancellationToken ct)
{
    return db.Customers
        .Where(c => c.IsActive && c.Region == "EU")                                     // duplicated predicates
        .Join(db.Orders, c => c.Id, o => o.CustomerId, (c, o) => new { c, o })          // anonymous<Customer, Order>
        .Join(db.Payments, x => x.o.Id, p => p.OrderId, (x, p) => new { x.c, x.o, p })  // anonymous<Customer, Order, Payment>
        .Where(x => x.o.CreatedAt >= from)
        .Select(x => new CustomerBalanceDto(x.c.Id, x.c.Name, x.p.Amount))  // mapping baked into DAL layer
        .ToArrayAsync(ct);                                                  // no pagination support
}

public Task<List<CustomerRiskDto>> GetRecentEuropeanCustomerRisksAsync(DateTime from, CancellationToken ct)
{
    return db.Customers
        .Where(c => c.IsActive && c.Region == "EU")                                     // duplicated predicates
        .Join(db.Orders, c => c.Id, o => o.CustomerId, (c, o) => new { c, o })          // anonymous<Customer, Order>
        .Join(db.Payments, x => x.o.Id, p => p.OrderId, (x, p) => new { x.c, x.o, p })  // anonymous<Customer, Order, Payment>
        .Where(x => x.o.CreatedAt >= from)
        .Where(x => x.p.Amount >= 10000)
        .Select(x => new CustomerRiskDto(x.c.Id, x.c.Name, risk: "High"))  // mapping baked into DAL layer
        .ToArrayAsync(ct);                                                 // no pagination support
}
```

## &#x1F44D; With QChain

Readable, reusable, and aligned with your domain. QChain keeps intermediate query shapes as named tuples instead of anonymous types.

```csharp
public IQuery<(Customer c, Order o, Payment p)> GetActiveEuropeanCustomerBalances(DateTime from)
{
    return db.Customers
        .Where(c => c.IsActive().And(c.FromEurope()))  //composable predicates
        .WithOrders(db.Orders.CreatedAfter(from))      // Tuple<(Customer c, Order o)>
        .WithPayments();                               // Tuple<(Customer c, Order o, Payment p)>
}

public IQuery<(Customer c, Order o, Payment p)> GetRecentEuropeanCustomerRisks(DateTime from)
{
    return db.Customers
        .Where(c => c.IsActive().And(c.FromEurope()))  //composable predicates
        .WithOrders(db.Orders.CreatedAfter(from))      // Tuple<(Customer c, Order o)>
        .WithPayments(db.Payments.AmountOver(10000));  // Tuple<(Customer c, Order o, Payment p)>
}
```

## &#x1F517; Calling End

Mapping and pagination compose externally. Query composition is reusable while execution concerns remain composable.

```csharp
var balances = await unitOfWork.Query(db => db.Customers
        .GetActiveEuropeanCustomerBalances(from)
        .Select(x => new CustomerBalanceDto(x.c.Id, x.c.Name, x.p.Amount))  // mapping remains at the calling layer
        .Page(index, size))                                                 // pagination is applied as a query extension 
    .ToArrayAsync(ct);

var risks = await unitOfWork.Query(db => db.Customers
        .GetRecentEuropeanCustomerRisks(from)
        .Select(x => new CustomerRiskDto(x.c.Id, x.c.Name, risk: "High"))  // mapping remains at the calling layer
        .Page(index, size))                                                // pagination is applied as a query extension 
    .ToArrayAsync(ct);
```

---

## &#x1F3D7;&#xFE0F; Basic Usage

Start from any `IQueryable<T>` and wrap it in a `Query<T>`.

```csharp
IQuery<Account> Accounts = new Query<Account>(db.Set<Account>());
IQuery<Order> Orders = new Query<Order>(db.Set<Order>());
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

## Use normal query composition.

```csharp
var activeEuropeanAccounts = await unitOfWork.Query(db => db.Accounts
        .Where(a => a.IsActive())                // predicate reuse
        .Where(a => a.Region == Region.Europe))  //Expression<Func<T, bool>>
    .ToArrayAsync(ct);
```

## Predicates can also be composed together: 

```csharp
var activeEuropeanAccounts = await unitOfWork.Query(db => db.Accounts
        .Where(a => a.IsActive().And(a.FromEurope()))) // Func<T, Predicate>
    .ToArrayAsync(ct);
```

## Reusable across joins.

```csharp
var activeEuropeanAccountOrders = await unitOfWork.Query(db =>
    {
        IQuery<(Account account, Order order)> accountOrders = db.Accounts
            .Join(db.Orders, a => a.AccountId, o => o.AccountId,
                 (a, o) => ValueTuple.Create(a, o));

        return accountOrders
            .Where(x => x.account.IsActive().And(x.order.InLastMonth()))
            .Select(x => x.order);
    })
    .ToArrayAsync(ct);
```

---

## &#x1F4E6; Packages

- **QChain** - Core abstractions and query pipeline
- **QChain.EntityFrameworkCore** - EF Core integration

---

## &#x1F527; Installation

```bash
dotnet add package QChain
```

For EF Core support:

```bash
dotnet add package QChain.EntityFrameworkCore
```

