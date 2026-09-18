using PCompose.Internal;
using PCompose.Visitors;
using System.Linq.Expressions;
using System.Reflection;

namespace PCompose;

public static class PredicateCompiler
{
    extension(Predicate predicate)
    {
        public Expression<Func<T, bool>> Compile<T>()
        {
            var parameter = Expression.Parameter(typeof(T), "x");

            var body = CompilePredicate(predicate, parameter);

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }
    }
    
    extension<T>(Func<T, Predicate> predicate)
    {
        public Expression<Func<T, bool>> Compile() =>
            predicate(default(T)!).Compile<T>();
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
        => condition.Parameters.Count == 1
            ? ApplyConditionByType(condition, root)
            : ApplyConditionByPosition(condition, root);

    private static Expression ApplyConditionByType(LambdaExpression condition, ParameterExpression root)
    {
        var parameter = condition.Parameters[0];
        if (parameter.Type == root.Type)
            return ReplaceExpressionVisitor.Replace(condition.Body, parameter, root);

        foreach (var member in GetMembers(root.Type))
        {
            var target = Expression.MakeMemberAccess(root, member);
            if (target.Type == parameter.Type)
                return ReplaceExpressionVisitor.Replace(condition.Body, parameter, target);
        }

        throw new InvalidOperationException(
            $"Invalid argument type: expected a member type of {root.Type.Name}, but received {parameter.Type.Name}.");
    }

    private static Expression ApplyConditionByPosition(LambdaExpression condition, ParameterExpression root)
    {
        var members = GetMembers(root.Type);

        if (condition.Parameters.Count > members.Length)
            throw new InvalidOperationException(
                $"Invalid number of arguments: expected at most {members.Length}, but received {condition.Parameters.Count}.");

        var replacements = new Dictionary<Expression, Expression>();

        for (var index = 0; index < condition.Parameters.Count; index++)
        {
            var parameter = condition.Parameters[index];
            var target = Expression.MakeMemberAccess(root, members[index]);

            if (target.Type != parameter.Type)
                throw new InvalidOperationException(
                    $"Invalid argument type at position {index + 1}: expected {target.Type.Name}, but received {parameter.Type.Name}.");

            replacements.Add(parameter, target);
        }

        return ReplaceExpressionVisitor.ReplaceMany(condition.Body, replacements);
    }

    private static MemberInfo[] GetMembers(Type type) =>
        type.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(member => member.MetadataToken)
            .Cast<MemberInfo>()
            .Concat(type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(member => member.MetadataToken))
            .ToArray();
}
