namespace PCompose.Internal;

internal sealed record AndPredicate(Predicate Left, Predicate Right)
    : Predicate;
