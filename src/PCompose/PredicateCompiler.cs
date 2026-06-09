using PCompose.Internal;
using PCompose.Visitors;
using System.Linq.Expressions;
using System.Reflection;

namespace PCompose;

public static class PredicateCompiler
{
    public static Expression<Func<T, bool>> Compile<T>(Func<T, Predicate> predicate)
    {
        var parameter = Expression.Parameter(typeof(T), "x");

        var tree = predicate(default(T)!);

        var body = Compile(tree, parameter);

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    public static Expression Compile(Predicate predicate, ParameterExpression root)
    {
        return predicate switch
        {
            ConditionPredicate c => ApplyCondition(c.Expression, root),

            AndPredicate a => Expression.AndAlso(
                Compile(a.Left, root),
                Compile(a.Right, root)),

            OrPredicate o => Expression.OrElse(
                Compile(o.Left, root),
                Compile(o.Right, root)),

            NotPredicate n => Expression.Not(Compile(n.Inner, root)),

            _ => throw new NotSupportedException(predicate.GetType().Name)
        };
    }

    private static Expression ApplyCondition(
        LambdaExpression condition,
        ParameterExpression root)
    {
        var entityType = condition.Parameters[0].Type;

        var target = FindMember(root, entityType);

        return ReplaceExpressionVisitor.Replace(condition.Body, condition.Parameters[0], target);
    }

    private static Expression FindMember(Expression root, Type entityType)
    {
        foreach (var member in root.Type.GetFields().Cast<MemberInfo>()
                     .Concat(root.Type.GetProperties()))
        {
            var memberType = member switch
            {
                FieldInfo f => f.FieldType,
                PropertyInfo p => p.PropertyType,
                _ => null
            };

            if (memberType == entityType)
                return Expression.MakeMemberAccess(root, member);
        }

        if (root.Type == entityType)
            return root;

        throw new InvalidOperationException(
            $"Could not map predicate type {entityType.Name} on {root.Type.Name}.");
    }
}
