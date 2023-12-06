using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// Schema descriptor attribute that provides common mechanism of applying directives on schema type.
///
/// NOTE: HotChocolate currently does not provide mechanism to apply those directives.
/// </summary>
public abstract class SchemaTypeDescriptorAttribute : Attribute
{
    public abstract void OnConfigure(IDescriptorContext context, ISchemaTypeDescriptor descriptor, Type type);
}
