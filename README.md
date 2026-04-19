# QChain

**Reusable, composable LINQ query pipelines**

QChain lets you build reusable, composable, and expressive query pipelines on top of LINQ.  
Instead of duplicating query logic across repositories and services, you define query fragments once and chain them together.

---

## ✨ Motivation

LINQ is powerful, but in real-world applications it often leads to:

- duplicated query logic
- hard-to-read query chains
- bloated repositories
- weak or limited specification patterns

QChain solves this by turning queries into **reusable, composable building blocks**.

---

## 🚀 Example

Instead of:

```csharp
var orders = db.Orders
    .Where(o => o.CustomerId == customerId)
    .Where(o => o.CreatedAt >= DateTime.UtcNow.AddDays(-30))
    .Include(o => o.Payments)
    .ToListAsync();
```

With QChain:

```csharp
var orders = repo.Query()
    .ForCustomer(customerId)
    .InLast30Days()
    .WithPayments()
    .ToListAsync();
```

Readable, reusable, and aligned with your domain.

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
