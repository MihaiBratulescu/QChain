
using PCompose.Internal;
using PCompose.Visitors;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

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

        if (root.Type.Assembly == typeof(ValueTuple).Assembly && typeof(ITuple).IsAssignableFrom(root.Type))
        {
            var matches = root.Type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Cast<MemberInfo>()
                .Concat(root.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                .Select(member => Expression.MakeMemberAccess(root, member))
                .Where(member => member.Type == parameter.Type)
                .ToArray();

            if (matches.Length > 1)
                throw new InvalidOperationException(
                    $"Ambiguous argument type: multiple tuple elements have type {parameter.Type.Name}.");

            if (matches.Length == 1)
                return ReplaceExpressionVisitor.Replace(condition.Body, parameter, matches[0]);
        }

        throw new InvalidOperationException(
            $"Invalid argument type: expected {root.Type.Name}, but received {parameter.Type.Name}.");
    }

}
