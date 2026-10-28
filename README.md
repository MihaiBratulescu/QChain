# QChain

[![NuGet - QChain](https://img.shields.io/nuget/vpre/QChain?label=QChain)](https://www.nuget.org/packages/QChain)
[![Tests](https://github.com/MihaiBratulescu/QChain/actions/workflows/tests.yml/badge.svg)](https://github.com/MihaiBratulescu/QChain/actions/workflows/tests.yml)
[![License](https://img.shields.io/github/license/MihaiBratulescu/QChain?label=License)](https://github.com/MihaiBratulescu/QChain/blob/master/LICENSE)

**Compose LINQ query pipelines with named tuples.**

Repositories often get a bad reputation because limited query reuse turns them into glorified query holders. QChain lets you define shared joins once and reuse them across domain contexts through named tuples and `IQueryable<T>`.

## Usage

The example below organizes a DAL into an application database context, domain repositories, and a unit of work.

Call `AsQuery()` on your `DbSet<T>` query roots to enable QChain’s tuple-based query composition.

```csharp
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public IQueryable<Account> Accounts => Set<Account>().AsQuery();
    public IQueryable<Address> Addresses => Set<Address>().AsQuery();
    public IQueryable<Order> Orders => Set<Order>().AsQuery();
    public IQueryable<OrderLine> OrderLines => Set<OrderLine>().AsQuery();

    public IQueryable<(Account account, Order order)> CustomerOrders() =>
        Accounts.Join(Orders, a => a.AccountId, o => o.AccountId, (a, o) => ValueTuple.Create(a, o));

    public IQueryable<(Account account, Order order, OrderLine line)> CustomerOrderLines() =>
        CustomerOrders().Join(OrderLines, x => x.order.OrderId, l => l.OrderId, (x, l) => ValueTuple.Create(x.account, x.order, l));
}
```

```csharp
// Accounting and shipping build different business queries from the same join.
public class AccountingRepository(IApplicationDbContext db) : IAccountingRepository
{
    public IQueryable<OverduePaymentDto> GetOverdueReport(DateTime asOf) =>
        db.CustomerOrders()
            .Where(x => x.order.IsNotPaid() && x.order.IsPastDue(asOf))
            .Select(x => new OverduePaymentDto(x.account, x.order));
}

public class ShippingRepository(IApplicationDbContext db) : IShippingRepository
{
    public IQueryable<DispatchDto> GetDispatchQueue() =>
        db.CustomerOrders()
            .Where(x => x.order.IsPaid() && x.order.IsNotShipped())
            .Select(x => new DispatchDto(x.account, x.order));
}

// Warehouse joins CustomerOrderLines() with addresses to produce a delivery list.
public class WarehouseRepository(IApplicationDbContext db) : IWarehouseRepository
{
    public IQueryable<DeliveryItemDto> GetDeliveryList() =>
        db.CustomerOrderLines()
            .Where(x => x.order.IsPaid() && x.line.IsNotDelivered())
            .Join(db.Addresses, x => x.account.AccountId, a => a.AccountId,
                (x, a) => new DeliveryItemDto(x.account, x.order, x.line, a));
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

For reusable predicates and expression composition, QChain can be paired with [NeinLinq](https://github.com/axelheer/nein-linq) or [LINQKit](https://github.com/scottksmith95/LINQKit). The business functions above, such as `IsNotPaid()` and `IsNotShipped()`, are application-defined and assume NeinLinq is configured for expression translation.

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
