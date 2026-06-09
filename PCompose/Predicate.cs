using PCompose.Internal;
using System.Linq.Expressions;

namespace PCompose;

public abstract record Predicate
{
    public static implicit operator Predicate(LambdaExpression expression)
        => new ConditionPredicate(expression);
}
