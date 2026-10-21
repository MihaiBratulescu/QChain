using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace QChain.Tests;

public class AsyncQueryProvider
{
    [Fact]
    public async Task Forwards_Translated_Expression_Cancellation_And_Original_Task()
    {
        var source = new[] { 1, 2, 3 }.AsQueryable();
        var expectedTask = Task.FromResult(2);
        var inner = new RecordingProvider(source.Provider, expectedTask);
        IQueryable<int> wrapped = new Query<int>(new QueryProvider(inner, new QueryShape<int>(source)));
        var query = wrapped.Select(value => ValueTuple.Create(value * 10, value));
        using var cancellation = new CancellationTokenSource();

        var task = query.CountAsync(value => value.Item1 > 10, cancellation.Token);

        Assert.Same(expectedTask, task);
        Assert.Equal(cancellation.Token, inner.CancellationToken);
        Assert.NotNull(inner.Expression);
        Assert.Equal(2, source.Provider.Execute<int>(inner.Expression));
        Assert.Equal(2, await task);
    }

    [Fact]
    public async Task Rejects_Async_Execution_When_Underlying_Provider_Is_Synchronous()
    {
        var query = new[] { 1, 2, 3 }.AsQueryable().AsQuery();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => query.CountAsync());

        Assert.Equal("The underlying query provider does not support asynchronous execution.", error.Message);
        Assert.Equal(3, query.Count());
    }

    private sealed class RecordingProvider(IQueryProvider inner, Task<int> result) : IAsyncQueryProvider
    {
        public Expression? Expression { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public IQueryable CreateQuery(Expression expression) => inner.CreateQuery(expression);
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => inner.CreateQuery<TElement>(expression);

        public object? Execute(Expression expression) => throw new InvalidOperationException("Synchronous execution was called.");
        public TResult Execute<TResult>(Expression expression) => throw new InvalidOperationException("Synchronous execution was called.");

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            Expression = expression;
            CancellationToken = cancellationToken;
            return (TResult)(object)result;
        }
    }
}
