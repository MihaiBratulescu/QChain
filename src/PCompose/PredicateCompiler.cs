
using PCompose.Internal;
using PCompose.Visitors;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace PCompose;

public static class PredicateCompiler
{
    public static Expression<Func<T, bool>> Compile<T>(Expression<Func<T, Predicate>> predicate)
    {
        var parameter = predicate.Parameters[0];
        var body = CompileExpression(predicate.Body, parameter);

        return Expression.Lambda<Func<T, bool>>(body, parameter);
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

    private static Expression ApplyCondition(LambdaExpression condition, Expression root, bool allowMemberLookup = true)
    {
        if (condition.Parameters.Count != 1)
            throw new InvalidOperationException(
                $"Invalid number of arguments: expected 1, but received {condition.Parameters.Count}.");

        var parameter = condition.Parameters[0];
        if (parameter.Type == root.Type)
            return ReplaceExpressionVisitor.Replace(condition.Body, parameter, root);

        if (allowMemberLookup && root.Type.Assembly == typeof(ValueTuple).Assembly && typeof(ITuple).IsAssignableFrom(root.Type))
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

    private static Expression CompileExpression(Expression expression, ParameterExpression root)
    {
        if (expression is UnaryExpression { NodeType: ExpressionType.Convert } conversion &&
            (conversion.Method is null || conversion.Method.DeclaringType == typeof(Predicate)))
            return CompileExpression(conversion.Operand, root);

        if (expression is MethodCallExpression call)
        {
            if (call.Method.DeclaringType == typeof(PredicateHandlers))
            {
                return call.Method.Name switch
                {
                    "And" => Expression.AndAlso(
                        CompileExpression(call.Arguments[0], root),
                        CompileExpression(call.Arguments[1], root)),
                    "Or" => Expression.OrElse(
                        CompileExpression(call.Arguments[0], root),
                        CompileExpression(call.Arguments[1], root)),
                    "Not" => Expression.Not(CompileExpression(call.Arguments[0], root)),
                    _ => throw new NotSupportedException(call.Method.Name)
                };
            }

            if (call.Method.IsDefined(typeof(ExtensionAttribute), false) &&
                typeof(LambdaExpression).IsAssignableFrom(call.Type) &&
                ReferencesParameter(call.Arguments[0], root))
            {
                var target = call.Arguments[0];
                // The factory returns a template; preserve its receiver path for substitution.
                var factory = call.Update(null, call.Arguments.Select((argument, index) =>
                    index == 0 ? Expression.Default(argument.Type) : argument));
                var condition = (LambdaExpression)Evaluate(factory, root)!;
                return ApplyCondition(condition, target, allowMemberLookup: false);
            }
        }

        return Evaluate(expression, root) switch
        {
            Predicate predicate => CompilePredicate(predicate, root),
            LambdaExpression condition => ApplyCondition(condition, root),
            _ => throw new InvalidOperationException("Expected a predicate or a predicate expression.")
        };
    }

    private static object? Evaluate(Expression expression, ParameterExpression root)
    {
        if (ReferencesParameter(expression, root))
            throw new InvalidOperationException("Predicate factory arguments cannot depend on the query element.");

        return Expression.Lambda<Func<object?>>(Expression.Convert(expression, typeof(object))).Compile()();
    }

    private static bool ReferencesParameter(Expression expression, ParameterExpression root)
    {
        var visitor = new ParameterReferenceVisitor(root);
        visitor.Visit(expression);
        return visitor.Found;
    }

    private sealed class ParameterReferenceVisitor(ParameterExpression root) : ExpressionVisitor
    {
        public bool Found { get; private set; }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == root)
                Found = true;
            return node;
        }
    }
}
