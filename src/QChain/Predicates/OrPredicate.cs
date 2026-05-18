namespace QChain.Predicates;

internal sealed record OrPredicate(Predicate Left, Predicate Right)
    : Predicate;