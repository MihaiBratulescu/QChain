using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace QChain.EntityFrameworkCore.Visitors;

internal sealed class ReferenceEqualityComparer : IEqualityComparer<Expression>
{
    public static readonly ReferenceEqualityComparer Instance = new();

    public bool Equals(Expression? x, Expression? y) => ReferenceEquals(x, y);

    public int GetHashCode(Expression obj) => RuntimeHelpers.GetHashCode(obj);
}
