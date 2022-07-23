using HotChocolate.Language;

namespace HotChocolate.Fusion;

public readonly struct Request
{
    public DocumentNode Document { get; }

    public ObjectValueNode? VariableValues { get; }

    public ObjectValueNode? Extensions { get; }
}
