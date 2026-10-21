using System.Reflection;
using System.Runtime.ExceptionServices;

namespace QChain.Internal.Helpers;

internal static class QueryReflection
{
    public static object InvokeUnwrapped(this MethodInfo method, object? target, object?[] arguments) =>
        Invoke(method, target, arguments);

    public static object Invoke(MethodInfo method, object? target, object?[] arguments)
    {
        try
        {
            return method.Invoke(target, arguments)!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}
