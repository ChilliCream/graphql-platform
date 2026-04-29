using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SharedRoot.Name;

/// <summary>
/// Descriptor for the <c>Name</c> value type owned by the <c>name</c>
/// subgraph.
/// </summary>
public sealed class NameType : ObjectType<Name>
{
    protected override void Configure(IObjectTypeDescriptor<Name> descriptor)
    {
        descriptor.Field(n => n.Id).Type<NonNullType<IdType>>();
        descriptor.Field(n => n.Brand).Type<NonNullType<StringType>>();
        descriptor.Field(n => n.Model).Type<NonNullType<StringType>>();
    }
}
