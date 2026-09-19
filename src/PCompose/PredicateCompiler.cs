
using PCompose.Internal;
using PCompose.Visitors;
using System.Linq.Expressions;

namespace PCompose;

public static class PredicateCompiler
{
    extension<T>(Func<T, Predicate> predicate)
    {
        public Expression<Func<T, bool>> Compile()
        {
            var parameter = Expression.Parameter(typeof(T), "x");

            var body = CompilePredicate(predicate(default(T)!), parameter);

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }
    }

    private static Expression CompilePredicate(Predicate predicate, ParameterExpression root)
    {
        return predicate switch
        {
            ConditionPredicate c => ApplyCondition(c.Expression, root),

            AndPredicate a => Expression.AndAlso(
                CompilePredicate(a.Left, root),
                CompilePredicate(a.Right, root)),

            OrPredicate o => Expression.OrElse(
                CompilePredicate(o.Left, root),
                CompilePredicate(o.Right, root)),

            NotPredicate n => Expression.Not(CompilePredicate(n.Inner, root)),
            _ => throw new NotSupportedException(predicate.GetType().Name)
        };
    }

    private static Expression ApplyCondition(LambdaExpression condition, ParameterExpression root)
    {
        if (condition.Parameters.Count != 1)
            throw new InvalidOperationException(
                $"Invalid number of arguments: expected 1, but received {condition.Parameters.Count}.");

        var parameter = condition.Parameters[0];
        if (parameter.Type == root.Type)
            return ReplaceExpressionVisitor.Replace(condition.Body, parameter, root);

        throw new InvalidOperationException(
            $"Invalid argument type: expected {root.Type.Name}, but received {parameter.Type.Name}.");
    }

}
