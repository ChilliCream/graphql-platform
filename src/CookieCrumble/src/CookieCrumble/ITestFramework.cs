using System.Reflection;

namespace CookieCrumble;

public interface ITestFramework
{
    bool IsValidTestMethod(MemberInfo? method);

    void ThrowTestException(string message);
}
