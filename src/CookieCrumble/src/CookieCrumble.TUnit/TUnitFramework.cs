using System.Reflection;
using TUnit.Core.Exceptions;

namespace CookieCrumble.TUnit;

public class TUnitFramework : ITestFramework
{
    public bool IsValidTestMethod(MemberInfo? method)
        => method?.GetCustomAttributes(typeof(TestAttribute)).Any() ?? false;

    public void ThrowTestException(string message)
    {
        throw new TUnitException(message);
    }
}
