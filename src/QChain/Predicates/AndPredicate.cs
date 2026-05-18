namespace QChain.Predicates;

internal sealed record AndPredicate(Predicate Left, Predicate Right)
    : Predicate;
