using System.Reflection;
using Xunit;
using Xunit.Sdk;

namespace CookieCrumble.Xunit;

public class XunitFramework : ITestFramework
{
    public bool IsValidTestMethod(MemberInfo? method)
        => IsFactTestMethod(method) || IsTheoryTestMethod(method);

    private static bool IsFactTestMethod(MemberInfo? method)
        => method?.GetCustomAttributes(typeof(FactAttribute)).Any() ?? false;

    private static bool IsTheoryTestMethod(MemberInfo? method)
        => method?.GetCustomAttributes(typeof(TheoryAttribute)).Any() ?? false;

    public void ThrowTestException(string message)
    {
        throw new XunitException(message);
    }
}
