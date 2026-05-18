using System.Linq.Expressions;

namespace QChain.Predicates;

internal sealed record ConditionPredicate(LambdaExpression Expression)
    : Predicate;
