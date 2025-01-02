namespace HotChocolate.Types;

// this is just a marker type for the fluent code-first api.
public sealed class StrictNonNullType<T> : NonNullType<T>
    where T : IType
{
    private StrictNonNullType() { }
}
