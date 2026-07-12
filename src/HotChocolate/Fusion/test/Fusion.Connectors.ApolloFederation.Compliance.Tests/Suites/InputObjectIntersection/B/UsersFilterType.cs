using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.InputObjectIntersection.B;

/// <summary>
/// Descriptor for the <c>UsersFilter</c> input object in subgraph
/// <c>b</c>.
/// </summary>
public sealed class UsersFilterType : InputObjectType<UsersFilter>
{
    protected override void Configure(IInputObjectTypeDescriptor<UsersFilter> descriptor)
    {
        descriptor.Name("UsersFilter");
        descriptor.Field(f => f.Offset).Type<IntType>();
        descriptor.Field(f => f.First).Type<NonNullType<IntType>>();
    }
}
