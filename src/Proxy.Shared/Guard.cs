using System.Runtime.CompilerServices;

namespace Proxy.Shared;

public static class Guard
{
    public static T AgainstNull<T>(T value, [CallerArgumentExpression("value")] string? name = null) where T : class?
    {
        if (null == value)
        {
            throw new ArgumentException($"{name} cannot be null");
        }

        return value;
    }
}