#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// <see cref="Payload"/> is used as a data transform structure for the execution engine and
/// the typesystem
/// </summary>
internal class Payload
{
    public Payload(object? result)
    {
        Result = result;
    }

    public object? Result { get; }
}
