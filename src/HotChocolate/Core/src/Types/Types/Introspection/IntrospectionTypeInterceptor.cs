#nullable enable

using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Types.Introspection.IntrospectionFields;

namespace HotChocolate.Types.Introspection;

internal sealed class IntrospectionTypeInterceptor : TypeInterceptor
{
    private readonly List<ObjectTypeConfiguration> _objectTypeConfigurations = [];
    private IDescriptorContext _context = null!;
    private ObjectTypeConfiguration? _queryTypeConfiguration;

    internal override uint Position => uint.MaxValue - 200;

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        _context = context;
    }

    public override void OnAfterCompleteName(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        if(completionContext.Type is ObjectType && configuration is ObjectTypeConfiguration typeDef)
        {
            _objectTypeConfigurations.Add(typeDef);
        }
    }

    public override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeConfiguration configuration,
        OperationType operationType)
    {
        if (operationType is OperationType.Query)
        {
            _queryTypeConfiguration = configuration;
        }
    }

    public override void OnBeforeCompleteTypes()
    {
        if (_queryTypeConfiguration is not null)
        {
            var position = 0;
            _queryTypeConfiguration.Fields.Insert(position++, CreateSchemaField(_context));
            _queryTypeConfiguration.Fields.Insert(position++, CreateTypeField(_context));
            _queryTypeConfiguration.Fields.Insert(position, CreateTypeNameField(_context));
        }

        foreach (var typeDef in _objectTypeConfigurations)
        {
            if (ReferenceEquals(_queryTypeConfiguration, typeDef))
            {
                continue;
            }

            typeDef.Fields.Insert(0, CreateTypeNameField(_context));
        }
    }
}
