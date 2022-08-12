using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp;

internal static class AssigmentBuilderExtensions
{
    public static MethodCallBuilder AddMethodCall(
        this AssignmentBuilder builder,
        string? methodName = null)
    {
        var methodCallBuilder = MethodCallBuilder
            .New()
            .SetDetermineStatement(false);

        if (methodName is not null)
        {
            methodCallBuilder.SetMethodName(methodName);
        }

        builder.SetRighthandSide(methodCallBuilder);

        return methodCallBuilder;
    }
}