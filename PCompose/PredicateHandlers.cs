using PCompose.Internal;
using System.Linq.Expressions;

namespace PCompose;

public static class PredicateHandlers
{
    extension<T>(Expression<Func<T, bool>> condition)
    {
        public Predicate And<N>(Expression<Func<N, bool>> next) => 
            new AndPredicate(
                new ConditionPredicate(condition), 
                new ConditionPredicate(next));

        public Predicate Or<N>(Expression<Func<N, bool>> next) => 
            new OrPredicate(
                new ConditionPredicate(condition),
                new ConditionPredicate(next));
    }

    extension(Predicate predicate)
    {
        public Predicate And<N>(Expression<Func<N, bool>> next) => 
            new AndPredicate(predicate, new ConditionPredicate(next));

        public Predicate Or<N>(Expression<Func<N, bool>> next) =>
            new OrPredicate(predicate, new ConditionPredicate(next));
    }
}
