#nullable enable

using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate;

internal sealed class SchemaNameTypeInterceptor(string schemaName) : TypeInterceptor
{
    private IDescriptorContext _context = null!;
    private bool _isSchemaNameEnabled;

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        _context = context;
        _isSchemaNameEnabled = context.Options.EnableSchemaNameDirective;
    }

    public override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        TypeSystemConfiguration configuration)
    {
        if (_isSchemaNameEnabled
            && configuration is SchemaTypeConfiguration schema)
        {
            var directiveTypeRef = _context.TypeInspector.GetTypeRef(typeof(SchemaNameDirectiveType));
            var directive = new DirectiveNode("schemaName", new ArgumentNode("value", schemaName));
            discoveryContext.Dependencies.Add(new TypeDependency(directiveTypeRef, TypeDependencyFulfilled.Completed));
            schema.Directives.Add(new DirectiveConfiguration(directive));
        }
    }

    private sealed class SchemaNameDirectiveType : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("schemaName");
            descriptor.Location(Types.DirectiveLocation.Schema);
            descriptor.Argument("value").Type<NonNullType<StringType>>();
        }
    }
}
