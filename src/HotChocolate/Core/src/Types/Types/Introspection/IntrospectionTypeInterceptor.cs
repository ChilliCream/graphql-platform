#nullable enable

using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Types.Introspection.IntrospectionFields;

namespace HotChocolate.Types.Introspection;

internal sealed class IntrospectionTypeInterceptor : TypeInterceptor
{
    private readonly List<ObjectTypeConfiguration> _objectTypeDefinitions = [];
    private IDescriptorContext _context = default!;
    private ObjectTypeConfiguration? _queryTypeDefinition;

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
            _objectTypeDefinitions.Add(typeDef);
        }
    }

    public override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeConfiguration configuration,
        OperationType operationType)
    {
        if (operationType is OperationType.Query)
        {
            _queryTypeDefinition = configuration;
        }
    }

    public override void OnBeforeCompleteTypes()
    {
        if (_queryTypeDefinition is not null)
        {
            var position = 0;
            _queryTypeDefinition.Fields.Insert(position++, CreateSchemaField(_context));
            _queryTypeDefinition.Fields.Insert(position++, CreateTypeField(_context));
            _queryTypeDefinition.Fields.Insert(position, CreateTypeNameField(_context));
        }

        foreach (var typeDef in _objectTypeDefinitions)
        {
            if (ReferenceEquals(_queryTypeDefinition, typeDef))
            {
                continue;
            }

            typeDef.Fields.Insert(0, CreateTypeNameField(_context));
        }
    }
}
