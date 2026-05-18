using System.Linq.Expressions;

namespace QChain.Predicates;

public abstract record Predicate
{
    public static implicit operator Predicate(LambdaExpression expression)
        => new ConditionPredicate(expression);
}
