namespace PCompose.Internal;

internal sealed record NotPredicate(Predicate Inner)
    : Predicate;
