using HotChocolate.Execution.Configuration;

namespace HotChocolate.Types.BatchResolvers;

public sealed record Declaration(Action<IRequestExecutorBuilder> Configure)
{
    public string? NotApplicableReason { get; private init; }

    public static Declaration NotApplicable(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new Declaration(_ => throw ThrowHelper.DeclarationNotApplicable(reason))
        {
            NotApplicableReason = reason
        };
    }
}
