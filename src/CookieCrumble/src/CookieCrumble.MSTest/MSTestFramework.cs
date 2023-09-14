using CookieCrumble;
using System.Reflection;

namespace CookieCrumble.MSTest;

public class MSTestFramework : ITestFramework
{
    public bool IsValidTestMethod(MemberInfo? method)
        => method?.GetCustomAttributes(typeof(Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute)).Any() ?? false;

    public void ThrowTestException(string message)
    {
        throw new Microsoft.VisualStudio.TestTools.UnitTesting.AssertFailedException(message);
    }
}
