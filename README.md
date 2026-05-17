[![NuGet - QChain](https://img.shields.io/nuget/v/QChain?label=QChain)](https://www.nuget.org/packages/QChain)
[![NuGet - QChain.EntityFrameworkCore](https://img.shields.io/nuget/v/QChain.EntityFrameworkCore?label=QChain.EntityFrameworkCore)](https://www.nuget.org/packages/QChain.EntityFrameworkCore)
[![License](https://img.shields.io/github/license/MihaiBratulescu/QChain?label=License)](https://github.com/MihaiBratulescu/QChain/blob/master/LICENSE)

# QChain

**LINQ specification pattern used to build reusable and composable query pipelines**

QChain lets you build reusable, composable, and expressive query pipelines on top of LINQ.  
Instead of duplicating query logic across repositories and services, you define query fragments once and chain them together.

---

## 👎 Before QChain

Large EF Core applications often end up with duplicated, tightly coupled query logic.

Reuse is hard because joins produce anonymous intermediate types.
Mapping is baked into repository methods, even when DTOs belong to upper layers.
Pagination, sorting, or extra filters require more repository methods or a wider API surface.

```csharp
public Task<List<CustomerBalanceDto>> GetActiveEuropeanCustomerBalancesAsync(DateTime from, CancellationToken ct)
{
    return db.Customers
        .Where(c => c.IsActive)
        .Where(c => c.Region == "EU")
        .Join(db.Orders.Where(o => o.CreatedAt >= from), c => c.Id, o => o.CustomerId, (c, o) => new { c, o }) // anonymous<Customer, Order>
        .Join(db.Payments, x => x.o.Id, p => p.OrderId, (x, p) => new { x.c, x.o, p })                         // anonymous<Customer, Order, Payment>
        .Select(x => new CustomerBalanceDto(x.c.Id, x.c.Name, x.p.Amount))                                     // mapping baked in
        .ToListAsync(ct);                                                                                      //no pagination support
}

public Task<List<CustomerRiskDto>> GetRecentEuropeanCustomerRisksAsync(DateTime from, CancellationToken ct)
{
    return db.Customers
        .Where(c => c.IsActive)
        .Where(c => c.Region == "EU")
        .Join(db.Orders, c => c.Id, o => o.CustomerId, (c, o) => new { c, o })            // anonymous<Customer, Order>
        .Join(db.Payments, x => x.o.Id, p => p.OrderId, (x, p) => new { x.c, x.o, p })    // anonymous<Customer, Order, Payment>
        .Where(x => x.o.CreatedAt >= from)
        .Where(x => x.p.Amount >= 10000)
        .Select(x => new CustomerRiskDto(x.c.Id, x.c.Name, risk: "High"))                 // mapping baked in
        .ToListAsync(ct);                                                                 //no pagination support
}
```

## 👍 With QChain

```csharp
public Task<List<(Customer c, Order o, Payment p)>> GetActiveEuropeanCustomerBalancesAsync(DateTime from, CancellationToken ct)
{
    return db.Customers
        .Active()
        .FromEurope()
        .WithOrders(o => .CreatedAfter(from)) // Tuple<(Customer c, Order o)>
        .WithPayments()                       // Tuple<(Customer c, Order o, Payment p)>
        .ToListAsync(ct);
}

public Task<List<(Customer c, Order o, Payment p)>> GetRecentEuropeanCustomerRisksAsync(DateTime from, CancellationToken ct)
{
    return db.Customers
        .Active()
        .FromEurope()
        .WithOrders(o => o.CreatedAfter(from))  // Tuple<(Customer c, Order o)>
        .WithPayments(p => p.AmountOver(10000)) // (Customer c, Order o, Payment p)
        .ToListAsync(ct);
}
```

Readable, reusable, and aligned with your domain.

---

## ✨ Motivation

LINQ is powerful, but in real-world applications it often leads to:

- duplicated query logic
- hard-to-read query chains
- bloated repositories
- weak or limited specification patterns

QChain solves this by turning queries into **reusable, composable building blocks**.

---


## 🧠 Key Concepts

### Composable Queries

Each method returns a query that can be further composed:

```csharp
public IOrdersQuery InLast30Days() =>
    Where(o => o.CreatedAt >= DateTime.UtcNow.AddDays(-30));
```

### Reusability

Query fragments can be reused across:

- repositories
- services
- endpoints

### Domain-Oriented

Queries can reflect your ubiquitous language:

```csharp
Orders
   .ForCustomer(id)
   .InLastMonth()
   .WithPayments(Payments
        .BankTransfers()
        .Settled())
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

---

## 🏗️ Basic Usage

```csharp
public interface IOrdersQuery : IQuery<Order>
{
    IOrdersQuery ForCustomer(int customerId);
    IOrdersQuery InLast30Days();
    IOrdersQuery WithPayments();
}
```

```csharp
public class OrdersQuery : EntityQuery<Order>, IOrdersQuery
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

## 🎯 Goals

- Keep LINQ as the execution engine
- Enable query reuse without duplication
- Improve readability of complex queries
- Align data access with domain language
- Stay provider-agnostic

---
