[![NuGet - QChain](https://img.shields.io/nuget/v/QChain?label=QChain)](https://www.nuget.org/packages/QChain)
[![NuGet - QChain.EntityFrameworkCore](https://img.shields.io/nuget/v/QChain.EntityFrameworkCore?label=QChain.EntityFrameworkCore)](https://www.nuget.org/packages/QChain.EntityFrameworkCore)
[![License](https://img.shields.io/github/license/MihaiBratulescu/QChain?label=License)](https://github.com/MihaiBratulescu/QChain/blob/master/LICENSE)

---

# QChain

**LINQ specification pattern for building reusable and composable query pipelines.**

QChain lets you build reusable, composable, and expressive query pipelines on top of LINQ.  
Instead of duplicating query logic across repositories and services, you define query fragments once and chain them together.

---

## ✨ Motivation

LINQ is powerful, but in real-world applications it often leads to:
- duplicated query logic
- hard-to-read query chains
- bloated repositories
- poor support for reusable specifications

### What QChain Solves
- Reusable composition
- Composable pipelines
- Flexible construction and execution

---

## 👎 Before QChain

Large EF Core applications often end up with long methods, and duplicated, tightly coupled query logic.

- Joins produce anonymous intermediate types, making query composition and reuse difficult.
- Mapping is either baked in or deferred until after execution, requiring full entities to be loaded.
- Pagination, sorting, or extra filters require more repository methods and a wider API surface.

```csharp
public Task<List<CustomerBalanceDto>> GetActiveEuropeanCustomerBalancesAsync(DateTime from, CancellationToken ct)
{
    return db.Customers
        .Where(c => c.IsActive)
        .Where(c => c.Region == "EU")
        .Join(db.Orders, c => c.Id, o => o.CustomerId, (c, o) => new { c, o })          // anonymous<Customer, Order>
        .Join(db.Payments, x => x.o.Id, p => p.OrderId, (x, p) => new { x.c, x.o, p })  // anonymous<Customer, Order, Payment>
        .Where(x => x.o.CreatedAt >= from)
        .Select(x => new CustomerBalanceDto(x.c.Id, x.c.Name, x.p.Amount))              // mapping baked into DAL layer
        .ToListAsync(ct);                                                               // no pagination support
}

public Task<List<CustomerRiskDto>> GetRecentEuropeanCustomerRisksAsync(DateTime from, CancellationToken ct)
{
    return db.Customers
        .Where(c => c.IsActive)
        .Where(c => c.Region == "EU")
        .Join(db.Orders, c => c.Id, o => o.CustomerId, (c, o) => new { c, o })          // anonymous<Customer, Order>
        .Join(db.Payments, x => x.o.Id, p => p.OrderId, (x, p) => new { x.c, x.o, p })  // anonymous<Customer, Order, Payment>
        .Where(x => x.o.CreatedAt >= from)
        .Where(x => x.p.Amount >= 10000)
        .Select(x => new CustomerRiskDto(x.c.Id, x.c.Name, risk: "High"))  // mapping baked into DAL layer
        .ToListAsync(ct);                                                  // no pagination support
}
```

## 👍 With QChain

Readable, reusable, and aligned with your domain. QChain keeps intermediate query shapes as named tuples instead of anonymous types.

```csharp
public IQuery<(Customer c, Order o, Payment p)> GetActiveEuropeanCustomerBalances(DateTime from)
{
    return db.Customers
        .Active()
        .FromEurope()
        .WithOrders(db.Orders.CreatedAfter(from))  // Tuple<(Customer c, Order o)>
        .WithPayments();                           // Tuple<(Customer c, Order o, Payment p)>
}

public IQuery<(Customer c, Order o, Payment p)> GetRecentEuropeanCustomerRisks(DateTime from)
{
    return db.Customers
        .Active()
        .FromEurope()
        .WithOrders(db.Orders.CreatedAfter(from))      // Tuple<(Customer c, Order o)>
        .WithPayments(db.Payments.AmountOver(10000));  // Tuple<(Customer c, Order o, Payment p)>
}
```

## 🔗 Calling End

Mapping and pagination compose externally. Query composition is reusable while execution concerns remain composable.

```csharp
var balances = await unitOfWork.Query(db => db.Customers
        .GetActiveEuropeanCustomerBalances(from)
        .Select(x => new CustomerBalanceDto(x.c.Id, x.c.Name, x.p.Amount))  // mapping remains at the calling layer
        .Page(page, size))                                               // pagination is applied as a query extension 
    .ToListAsync(ct);

var risks = await unitOfWork.Query(db => db.Customers
        .GetRecentEuropeanCustomerRisks(from)
        .Select(x => new CustomerRiskDto(x.c.Id, x.c.Name, risk: "High"))  // mapping remains at the calling layer
        .Page(page, size))                                              // pagination is applied as a query extension 
    .ToListAsync(ct);
```

---

## 🏗️ Basic Usage

```csharp
public interface IOrdersRepository : IQuery<Order>
{
    IOrdersQuery ForCustomer(int customerId);
    IOrdersQuery InLast30Days();
    IOrdersQuery WithPayments();
}
```

```csharp
public class OrdersRepository : Query<Order>, IOrdersQuery
{
    public OrdersQuery(IQueryable<Order> source) : base(source) { }

    public IOrdersQuery ForCustomer(int customerId) =>
        new OrdersQuery(Where(o => o.CustomerId == customerId).AsQueryable());

    public IOrdersQuery InLast30Days() =>
        new OrdersQuery(Where(o => o.CreatedAt >= DateTime.UtcNow.AddDays(-30)).AsQueryable());

    public IOrdersQuery WithPayments() =>
        new OrdersQuery(Include(o => o.Payments).AsQueryable());
}
```

---

## 📦 Packages

- **QChain** – Core abstractions and query pipeline
- **QChain.EntityFrameworkCore** – EF Core integration

---

## 🔧 Installation

```bash
dotnet add package QChain
```

For EF Core support:

```bash
dotnet add package QChain.EntityFrameworkCore
```

