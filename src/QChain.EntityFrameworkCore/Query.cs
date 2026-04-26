using QChain.EntityFrameworkCore.Visitors;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain.EntityFrameworkCore;

public class Query<T, Q> : IQuery<T>, IOrderedQuery<T>, IInternalQuery
{
    #region Internal Query
    protected IQueryable<Q> Source { get; }
    protected Expression<Func<Q, T>> Shape { get; }

    public IQueryable UntypedSource => Source;
    public LambdaExpression UntypedShape => Shape;
    #endregion

    protected Query(IQueryable<Q> source, Expression<Func<Q, T>> shape) =>
        (Source, Shape) = (source, shape);

    protected Query(Query<T, Q> query) =>
        (Source, Shape) = (query.Source, query.Shape);

    public IQueryable<T> AsQueryable() => Source.Select(Shape);

    #region Filtering
    public IQuery<T> Where(Expression<Func<T, bool>> predicate) =>
        new Query<T, Q>(Source.Where(Translate(predicate)), Shape);

    public IQuery<T> Distinct() =>
        new Query<T, T>(Source.Select(Shape).Distinct(), x => x);

    public IQuery<R> DistinctBy<R>(Expression<Func<T, R>> selector) =>
        new Query<R, R>(Source.Select(Compose(selector, Shape)).Distinct(), x => x);
    #endregion

    #region Grouping
    public IQuery<(K Key, IEnumerable<T> Items)> GroupBy<K>(Expression<Func<T, K>> selector) =>
        new Query<(K, IEnumerable<T>), IGrouping<K, Q>>(Source.GroupBy(Translate(selector)),
            g => ValueTuple.Create(g.Key, g.AsQueryable().Select(Shape).AsEnumerable()));

    public IQuery<R> GroupBy<K, R>(Expression<Func<T, K>> key, Expression<Func<IGrouping<K, T>, R>> selector) =>
        new Query<R, R>(Source.GroupBy(Translate(key)).Select(TranslateGroup(selector)), x => x);

    public IQuery<IGrouping<K, R>> GroupBy<K, R>(Expression<Func<T, K>> key, Expression<Func<T, R>> selector)
    {
        Expression<Func<Q, K>> keySelector = Translate(key);
        Expression<Func<Q, R>> elementSelector = Translate(selector);

        return new Query<IGrouping<K, R>, IGrouping<K, R>>(
            Source.GroupBy(keySelector, elementSelector),
            q => q);
    }

    #endregion

    #region Projection
    public IQuery<R> Map<R>(Expression<Func<T, R>> mapping) =>
        new Query<R, Q>(Source, Compose(mapping, Shape));

    public IQuery<R> Flatten<R>(Expression<Func<T, IEnumerable<R>>> collectionSelector) =>
        FlattenPreservingShape<R>(Translate(collectionSelector));
    #endregion

    #region Sorting
    public IOrderedQuery<T> OrderBy<K>(Expression<Func<T, K>> selector) =>
        new Query<T, Q>(Source.OrderBy(Translate(selector)), Shape);

    public IOrderedQuery<T> OrderByDescending<K>(Expression<Func<T, K>> selector) =>
        new Query<T, Q>(Source.OrderByDescending(Translate(selector)), Shape);

    public IOrderedQuery<T> ThenBy<TKey>(Expression<Func<T, TKey>> selector) =>
        new Query<T, Q>((Source as IOrderedQueryable<Q>)!.ThenBy(Translate(selector)), Shape);

    public IOrderedQuery<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> selector) =>
        new Query<T, Q>((Source as IOrderedQueryable<Q>)!.ThenByDescending(Translate(selector)), Shape);
    #endregion

    #region Joins
    public IQuery<(T, R)> Join<R, K>(IQuery<R> right, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey) =>
        Join(right, lKey, rKey, (left, rightRow) => ValueTuple.Create(left, rightRow));

    public IQuery<TOut> Join<R, K, TOut>(IQuery<R> right, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey, Expression<Func<T, R, TOut>> result) =>
        JoinInternal((right as IInternalQuery)!, lKey, rKey, result);

    public IQuery<(T, IEnumerable<R>)> GroupJoin<R, K>(IQuery<R> right, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey) =>
        GroupJoin(right, lKey, rKey, (t, items) => ValueTuple.Create(t, items));

    public IQuery<TOut> GroupJoin<R, K, TOut>(IQuery<R> right, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey, Expression<Func<T, IEnumerable<R>, TOut>> result) =>
        GroupJoinInternal((right as IInternalQuery)!, lKey, rKey, result);
    #endregion

    #region Caching
    public ICachedQuery<T> WithCaching(string key, TimeSpan expiry) =>
        new CachedQuery<T, Q>(this, key, expiry);
    #endregion

    #region Helpers
    private Expression<Func<Q, TResult>> Translate<TResult>(Expression<Func<T, TResult>> expression)
    {
        var body = new ProjectionInliningVisitor(expression.Parameters[0], Shape.Body).Visit(expression.Body)!;

        body = new TupleAccessSimplifyingVisitor().Visit(body)!;

        return Expression.Lambda<Func<Q, TResult>>(body, Shape.Parameters);
    }

    private Expression<Func<IGrouping<G, Q>, R>> TranslateGroup<G, R>(Expression<Func<IGrouping<G, T>, R>> selector)
    {
        var groupQ = Expression.Parameter(typeof(IGrouping<G, Q>), selector.Parameters[0].Name);
        var visitor = new GroupTranslateVisitor<G, Q, T>(groupQ, selector.Parameters[0], Shape);

        return Expression.Lambda<Func<IGrouping<G, Q>, R>>(visitor.Visit(selector.Body), groupQ);
    }

    private static Expression<Func<TSource, TResult>> Compose<TSource, TMiddle, TResult>(Expression<Func<TMiddle, TResult>> outer, Expression<Func<TSource, TMiddle>> inner)
    {
        var body = ReplaceExpressionVisitor.Replace(outer.Body, outer.Parameters[0], inner.Body);

        body = new TupleAccessSimplifyingVisitor().Visit(body)!;

        return Expression.Lambda<Func<TSource, TResult>>(body, inner.Parameters);
    }

    private static MethodCallExpression ComposeEnumerable<TRightInternal, TRightPublic>(Expression<Func<TRightInternal, TRightPublic>> itemShape, Expression enumerableExpression)
    {
        return Expression.Call(
            EnumerableSelectMethod.MakeGenericMethod(typeof(TRightInternal), typeof(TRightPublic)),
            enumerableExpression,
            itemShape);
    }

    private IQuery<R> FlattenPreservingShape<R>(LambdaExpression translatedCollectionSelector)
    {
        var call = translatedCollectionSelector.Body as MethodCallExpression;

        var sourceExpression = call.Arguments[0];
        var selectorExpression = Unquote(call.Arguments[1]);

        var internalCollectionType = typeof(IEnumerable<>).MakeGenericType(selectorExpression.Parameters[0].Type);
        var internalCollectionSelector = Expression.Lambda(
            typeof(Func<,>).MakeGenericType(translatedCollectionSelector.Parameters[0].Type, internalCollectionType),
            sourceExpression,
            translatedCollectionSelector.Parameters);


        var generic = FlattenPreservingShapeTypedMethod.MakeGenericMethod(typeof(R), selectorExpression.Parameters[0].Type);

        return (IQuery<R>)generic.Invoke(this, [internalCollectionSelector, selectorExpression])!;
    }

    private Query<R, QR> FlattenPreservingShapeTyped<R, QR>(LambdaExpression internalCollectionSelectorUntyped, LambdaExpression itemShapeUntyped)
    {
        var internalCollectionSelector = (Expression<Func<Q, IEnumerable<QR>>>)internalCollectionSelectorUntyped;
        var itemShape = (Expression<Func<QR, R>>)itemShapeUntyped;

        return new Query<R, QR>(Source.SelectMany(internalCollectionSelector), itemShape);
    }

    private IQuery<TOut> JoinInternal<R, K, TOut>(IInternalQuery right, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey, Expression<Func<T, R, TOut>> result)
    {
        var qr = right.UntypedShape.Parameters[0].Type;

        var generic = JoinInternalTypedMethod.MakeGenericMethod(typeof(R), typeof(K), typeof(TOut), qr);

        return (IQuery<TOut>)generic.Invoke(this, [right, lKey, rKey, result])!;
    }

    private Query<TOut, Pair<Q, QR>> JoinInternalTyped<R, K, TOut, QR>(IInternalQuery rightUntyped, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey, Expression<Func<T, R, TOut>> result)
    {
        var right = (Query<R, QR>)rightUntyped;

        var joined = Source.Join(right.Source, Translate(lKey), right.Translate(rKey),
            (l, r) => new Pair<Q, QR> { Left = l, Right = r });

        return new Query<TOut, Pair<Q, QR>>(joined, BuildJoinShape(right.Shape, result));
    }

    private Expression<Func<Pair<Q, QR>, TOut>> BuildJoinShape<R, QR, TOut>(Expression<Func<QR, R>> rightShape, Expression<Func<T, R, TOut>> result)
    {
        var pairParam = Expression.Parameter(typeof(Pair<Q, QR>), "p");

        var leftInternal = Expression.Property(pairParam, nameof(Pair<Q, QR>.Left));
        var rightInternal = Expression.Property(pairParam, nameof(Pair<Q, QR>.Right));

        var leftPublic = ReplaceExpressionVisitor.Replace(Shape.Body, Shape.Parameters[0], leftInternal);

        var rightPublic = ReplaceExpressionVisitor.Replace(rightShape.Body, rightShape.Parameters[0], rightInternal);

        var body = ReplaceExpressionVisitor.ReplaceMany(result.Body, new Dictionary<Expression, Expression>
        {
            [result.Parameters[0]] = leftPublic,
            [result.Parameters[1]] = rightPublic
        });

        return Expression.Lambda<Func<Pair<Q, QR>, TOut>>(body, pairParam);
    }

    private IQuery<TOut> GroupJoinInternal<R, K, TOut>(IInternalQuery right, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey, Expression<Func<T, IEnumerable<R>, TOut>> result)
    {
        var qr = right.UntypedShape.Parameters[0].Type;

        var generic = GroupJoinInternalTypedMethod.MakeGenericMethod(typeof(R), typeof(K), typeof(TOut), qr);

        return (IQuery<TOut>)generic.Invoke(this, [right, lKey, rKey, result])!;
    }

    private Query<TOut, Pair<Q, IEnumerable<QR>>> GroupJoinInternalTyped<R, K, TOut, QR>(IInternalQuery rightUntyped, Expression<Func<T, K>> lKey, Expression<Func<R, K>> rKey, Expression<Func<T, IEnumerable<R>, TOut>> result)
    {
        var right = (Query<R, QR>)rightUntyped;

        var grouped = Source.GroupJoin(right.Source, Translate(lKey), right.Translate(rKey),
            (l, r) => new Pair<Q, IEnumerable<QR>> { Left = l, Right = r });

        return new Query<TOut, Pair<Q, IEnumerable<QR>>>(grouped, BuildGroupJoinShape(right.Shape, result));
    }

    private Expression<Func<Pair<Q, IEnumerable<QR>>, TOut>> BuildGroupJoinShape<R, QR, TOut>(Expression<Func<QR, R>> rightShape, Expression<Func<T, IEnumerable<R>, TOut>> result)
    {
        var pairParam = Expression.Parameter(typeof(Pair<Q, IEnumerable<QR>>), "p");

        var leftInternal = Expression.Property(pairParam, nameof(Pair<Q, IEnumerable<QR>>.Left));
        var rightInternal = Expression.Property(pairParam, nameof(Pair<Q, IEnumerable<QR>>.Right));

        var leftPublic = ReplaceExpressionVisitor.Replace(Shape.Body, Shape.Parameters[0], leftInternal);

        var projectedRight = ComposeEnumerable(rightShape, rightInternal);

        var body = ReplaceExpressionVisitor.ReplaceMany(result.Body, new Dictionary<Expression, Expression>
        {
            [result.Parameters[0]] = leftPublic,
            [result.Parameters[1]] = projectedRight
        });

        return Expression.Lambda<Func<Pair<Q, IEnumerable<QR>>, TOut>>(body, pairParam);
    }

    private static LambdaExpression Unquote(Expression expression)
    {
        return expression is UnaryExpression { NodeType: ExpressionType.Quote, Operand: LambdaExpression lambda }
            ? lambda
            : (LambdaExpression)expression;
    }

    private static readonly MethodInfo JoinInternalTypedMethod = typeof(Query<T, Q>).GetMethod(nameof(JoinInternalTyped), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo GroupJoinInternalTypedMethod = typeof(Query<T, Q>).GetMethod(nameof(GroupJoinInternalTyped), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo FlattenPreservingShapeTypedMethod = typeof(Query<T, Q>).GetMethod(nameof(FlattenPreservingShapeTyped), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo EnumerableSelectMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(Enumerable.Select) &&
                     m.IsGenericMethodDefinition &&
                     m.GetParameters()[1].ParameterType is { IsGenericType: true } p &&
                     p.GetGenericTypeDefinition() == typeof(Func<,>));
    #endregion

    private readonly struct Pair<T1, T2>
    {
        public required T1 Left { get; init; }
        public required T2 Right { get; init; }
    }
}