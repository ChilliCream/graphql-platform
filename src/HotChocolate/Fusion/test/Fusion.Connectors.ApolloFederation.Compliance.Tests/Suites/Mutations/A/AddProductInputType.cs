using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Mutations.A;

/// <summary>
/// Descriptor for the <c>AddProductInput</c> input object on subgraph <c>a</c>.
/// </summary>
public sealed class AddProductInputType : InputObjectType<AddProductInput>
{
    protected override void Configure(IInputObjectTypeDescriptor<AddProductInput> descriptor)
    {
        descriptor.Field(i => i.Name).Type<NonNullType<StringType>>();
        descriptor.Field(i => i.Price).Type<NonNullType<FloatType>>();
    }
}
