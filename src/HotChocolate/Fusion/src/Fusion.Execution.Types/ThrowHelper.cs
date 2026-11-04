namespace HotChocolate.Fusion.Types;

internal static class ThrowHelper
{
    public static void EnsureNotSealed(bool completed)
    {
        if (completed)
        {
            throw new NotSupportedException(
                "The type definition is sealed and cannot be modified.");
        }
    }

    public static NotSupportedException TypeSystemMemberSealed()
        => new NotSupportedException(
            "The type system member is sealed and cannot be modified.");

    public static InvalidOperationException InvalidCompletionContext()
        => new("The context has an invalid state.");

    public static InvalidOperationException InvalidCostOptionsArgument(
        string argumentName,
        string requirement)
        => new($"The fusion__cost_options {argumentName} argument must be {requirement}.");

    public static InvalidOperationException InvalidCostSetting(string path, string requirement)
        => new($"The {path} setting must be {requirement}.");

    public static InvalidOperationException InvalidPlannerSetting(string path, string requirement)
        => new($"The {path} setting must be {requirement}.");

    public static InvalidOperationException DuplicateCostOptionsDirective()
        => new("The fusion__cost_options directive may only be applied once per schema.");

    public static InvalidOperationException DuplicateExecutionDirective()
        => new("The fusion__execution directive may only be applied once per schema.");

    public static InvalidOperationException InvalidExecutionArgument(string argumentName, string requirement)
        => new($"The fusion__execution {argumentName} argument must be {requirement}.");
}
