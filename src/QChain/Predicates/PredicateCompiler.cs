using QChain.Predicates;
using QChain.Visitors;
using System.Linq.Expressions;
using System.Reflection;

namespace QChain.Predicates;

public static class PredicateCompiler
{
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

            _ => throw new NotSupportedException(predicate.GetType().Name)
        };
    }

    private static Expression ApplyCondition(
        LambdaExpression condition,
        ParameterExpression root)
    {
        var entityType = condition.Parameters[0].Type;

        var target = FindMember(root, entityType);

        return new ReplaceExpressionVisitor(
            condition.Parameters[0],
            target)
            .Visit(condition.Body)!;
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
