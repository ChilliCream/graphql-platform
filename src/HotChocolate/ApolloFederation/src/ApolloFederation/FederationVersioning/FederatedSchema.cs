using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// Apollo Federation base schema object that allows users to apply custom schema directives (e.g. @composeDirective)
/// </summary>
public abstract class FederatedSchema : Schema
{
    /// <summary>
    /// Initializes new instance of <see cref="FederatedSchema"/>
    /// </summary>
    /// <param name="version">
    /// Supported Apollo Federation version
    /// </param>
    protected FederatedSchema(FederationVersion version = FederationVersion.Latest)
    {
        FederationVersion = version;
    }

    /// <summary>
    /// Retrieve supported Apollo Federation version
    /// </summary>
    public FederationVersion FederationVersion { get; }

    private IDescriptorContext _context = default!;
    protected override void OnAfterInitialize(ITypeDiscoveryContext context, DefinitionBase definition)
    {
        base.OnAfterInitialize(context, definition);
        _context = context.DescriptorContext;
    }

    protected override void Configure(ISchemaTypeDescriptor descriptor)
    {
        var schemaType = this.GetType();
        if (schemaType.IsDefined(typeof(SchemaTypeDescriptorAttribute), inherit: true))
        {
            foreach (var attribute in schemaType.GetCustomAttributes(inherit: true))
            {
                if (attribute is SchemaTypeDescriptorAttribute casted)
                {
                    casted.OnConfigure(_context, descriptor, schemaType);
                }
            }
        }
        var link = FederationUtils.GetFederationLink(FederationVersion);
        descriptor.Link(link.Url, link.Import);
    }
}
