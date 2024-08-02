using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation.Types;

internal sealed class FieldSetAttribute : DirectiveArgumentDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IDirectiveArgumentDescriptor descriptor,
        PropertyInfo property)
        => descriptor.Type<NonNullType<FieldSetType>>();
}
