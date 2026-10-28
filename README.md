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

Here is one way to structure a DAL: the application database context exposes query roots and shared joins, repositories express business queries, and the unit of work exposes those repositories.

Define the relationship once, reuse it across DDD contexts.

```csharp
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public IQueryable<Account> Accounts => Set<Account>().AsQuery();
    public IQueryable<Order> Orders => Set<Order>().AsQuery();
    public IQueryable<OrderLine> OrderLines => Set<OrderLine>().AsQuery();

    public IQueryable<(Account account, Order order)> CustomerOrders() =>
        Accounts.Join(Orders, a => a.AccountId, o => o.AccountId, (a, o) => ValueTuple.Create(a, o));

    public IQueryable<(Account account, Order order, OrderLine line)> CustomerOrderLines() =>
        CustomerOrders().Join(OrderLines, x => x.order.OrderId, l => l.OrderId, (x, l) => ValueTuple.Create(x.account, x.order, l));
}
```

Accounting and shipping build different business queries from the same join:

```csharp
public class AccountingRepository(IApplicationDbContext db) : IAccountingRepository
{
    public IQueryable<OverduePaymentDto> GetOverdueReport(DateTime asOf) =>
        db.CustomerOrders().Where(x => x.order.IsNotPaid() && x.order.IsPastDue(asOf)).Select(x => new OverduePaymentDto(x.account, x.order));
}

public class ShippingRepository(IApplicationDbContext db) : IShippingRepository
{
    public IQueryable<DispatchDto> GetDispatchQueue() =>
        db.CustomerOrders().Where(x => x.order.IsPaid() && x.order.IsNotShipped()).Select(x => new DispatchDto(x.account, x.order));
}
```

Warehouse uses `CustomerOrderLines()` to produce a delivery list:

```csharp
public class WarehouseRepository(IApplicationDbContext db) : IWarehouseRepository
{
    public IQueryable<DeliveryItemDto> GetDeliveryList() =>
        db.CustomerOrderLines().Where(x => x.order.IsPaid() && x.line.IsNotDelivered()).Select(x => new DeliveryItemDto(x.account, x.order, x.line));
}
```

Expose repositories through the unit of work:

```csharp
public class UnitOfWork(IApplicationDbContext db) : IUnitOfWork
{
    public IAccountingRepository Accounting { get; } = new AccountingRepository(db);
    public IShippingRepository Shipping { get; } = new ShippingRepository(db);
    public IWarehouseRepository Warehouse { get; } = new WarehouseRepository(db);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
```

Callers execute the queries when needed:

```csharp
var overdue = await unitOfWork.Accounting.GetOverdueReport(asOf).ToArrayAsync(ct);
var dispatch = await unitOfWork.Shipping.GetDispatchQueue().ToArrayAsync(ct);
var deliveries = await unitOfWork.Warehouse.GetDeliveryList().ToArrayAsync(ct);
```

Business functions such as `IsNotPaid()` and `IsNotShipped()` are application-defined and configured with [NeinLinq](https://github.com/axelheer/nein-linq) for expression translation. QChain keeps the joined pipelines composable.

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
