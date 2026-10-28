# QChain

[![NuGet - QChain](https://img.shields.io/nuget/vpre/QChain?label=QChain)](https://www.nuget.org/packages/QChain)
[![Tests](https://github.com/MihaiBratulescu/QChain/actions/workflows/tests.yml/badge.svg)](https://github.com/MihaiBratulescu/QChain/actions/workflows/tests.yml)
[![License](https://img.shields.io/github/license/MihaiBratulescu/QChain?label=License)](https://github.com/MihaiBratulescu/QChain/blob/master/LICENSE)

**Composable LINQ query pipelines with named tuples.**

QChain lets you return joined queries from methods and keep composing them through `IQueryable<T>`. Define a shared pipeline once, then add filters, sorting, pagination, and projections where you use it.

## Motivation

Repositories can grow bloated with methods that repeat the same joins and differ only in their filters or projections. Sharing the common query is awkward when its intermediate results use anonymous types; returning it from a method means introducing a class or record just to hold those results.

QChain lets you use named tuples instead. Return a joined pipeline as `IQueryable<(Account account, Order order)>`, then build on it with familiar LINQ operators.

For reusable predicates and expression composition, QChain can be paired with a library such as [NeinLinq](https://github.com/axelheer/nein-linq) or [LINQKit](https://github.com/scottksmith95/LINQKit).

---

## &#x1F3D7;&#xFE0F; Basic Usage

Expose query roots with `AsQuery()` and define shared joins on the application database context. `IApplicationDbContext` exposes those roots and joined pipelines to repositories.

```csharp
using Microsoft.EntityFrameworkCore;
using QChain;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public IQueryable<Account> Accounts => Set<Account>().AsQuery();
    public IQueryable<Order> Orders => Set<Order>().AsQuery();

    public IQueryable<(Account account, Order order)> CustomerOrders() =>
        Accounts.Join(Orders, a => a.AccountId, o => o.AccountId, (a, o) => ValueTuple.Create(a, o));
}
```

`CustomerOrders()` represents a shared business relationship. Customer-facing queries and accounting queries can both build on it.

## Department Repositories

Organize repositories around business responsibilities. Each repository receives `IApplicationDbContext` and builds business queries from its roots and shared joins.

```csharp
public class AccountingRepository(IApplicationDbContext db) : IAccountingRepository
{
    private IQueryable<(Account account, Order order)> PendingPayments() =>
        db.CustomerOrders().Where(x => !x.order.IsPaid());

    private IQueryable<(Account account, Order order)> OverduePayments(DateTime asOf) =>
        PendingPayments().Where(x => x.order.IsInvoiced() && x.order.IsPastDue(asOf));

    public IQueryable<OverduePaymentDto> GetOverdueReport(DateTime asOf) =>
        OverduePayments(asOf).OrderBy(x => x.order.DueDate).ThenBy(x => x.order.OrderId).Select(x => new OverduePaymentDto(x.account, x.order));
}
```

The joined pipeline is narrowed by reusable business filters, then projected through a DTO constructor that receives each tuple member as a whole.

`IsPaid()`, `IsInvoiced()`, and `IsPastDue(asOf)` are application-defined business functions. This example assumes they are configured for translation with NeinLinq. QChain provides the tuple-based pipeline composition; expression libraries handle reusable business expressions.

## Unit of Work

Expose department repositories from the unit of work and share the same application database context between them.

```csharp
public class UnitOfWork(IApplicationDbContext db) : IUnitOfWork
{
    public IAccountingRepository Accounting { get; } = new AccountingRepository(db);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
```

Consumers use the department's business API and can continue composing before execution:

```csharp
var report = await unitOfWork.Accounting.GetOverdueReport(asOf).Skip(pageIndex * pageSize).Take(pageSize).ToArrayAsync(ct);
```

The query remains an `IQueryable<T>` until a terminal operation such as `ToArrayAsync(ct)` executes it. Keep the underlying context alive until execution completes.

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
