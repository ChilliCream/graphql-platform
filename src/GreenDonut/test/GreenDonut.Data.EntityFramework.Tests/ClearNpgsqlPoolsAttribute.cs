using System.Reflection;
using Npgsql;
using Xunit.v3;

[assembly: GreenDonut.Data.ClearNpgsqlPools]

namespace GreenDonut.Data;

/// <summary>
/// Ensures idle pooled connections to the per-test databases close after each test.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ClearNpgsqlPoolsAttribute : BeforeAfterTestAttribute
{
    public override void After(MethodInfo methodUnderTest, IXunitTest test)
        => NpgsqlConnection.ClearAllPools();
}
