namespace HotChocolate.Types;

[MutationType]
public partial class NonStaticMutation
{
    [GraphQLIgnore]
    public string InstanceId { get; } = "non-static-mutation";

    [UseMutationConvention]
    public RenameShapePayload RenameShape(string name)
        => new(InstanceId + ":" + name);
}

public sealed record RenameShapePayload(string Result);
