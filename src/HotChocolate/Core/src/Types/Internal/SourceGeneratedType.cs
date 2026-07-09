using HotChocolate.Types;

namespace HotChocolate.Internal;

/// <summary>
/// Internal marker type for source generated types.
/// </summary>
public sealed class SourceGeneratedType<T> where T : IType
{
    private SourceGeneratedType() { }
}
