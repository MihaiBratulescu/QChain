using System.Linq.Expressions;

namespace PCompose.Internal;

internal sealed record ConditionPredicate(LambdaExpression Expression)
    : Predicate;
