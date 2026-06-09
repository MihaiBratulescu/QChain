namespace PCompose.Internal;

internal sealed record OrPredicate(Predicate Left, Predicate Right)
    : Predicate;