#if NET10_0_OR_GREATER

using System.Linq.Expressions;

namespace QChain.Internal;

internal abstract partial class SequenceQueryShape<T, Q>
{
    public IQueryShape LeftJoin<R, K, TOut>(
        IQueryShape right,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T, R?, TOut>> result) =>
        JoinShapeBuilder<T, Q>.LeftJoin(this, right, leftKey, rightKey, result);

    public IQueryShape RightJoin<R, K, TOut>(
        IQueryShape right,
        Expression<Func<T, K>> leftKey,
        Expression<Func<R, K>> rightKey,
        Expression<Func<T?, R, TOut>> result) =>
        JoinShapeBuilder<T, Q>.RightJoin(this, right, leftKey, rightKey, result);
}

#endif
