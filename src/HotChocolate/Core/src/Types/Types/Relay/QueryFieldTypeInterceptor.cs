using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.WellKnownContextData;

#nullable enable

namespace HotChocolate.Types.Relay;

internal sealed class QueryFieldTypeInterceptor : TypeInterceptor
{
    private const string _defaultFieldName = "query";
    private readonly HashSet<string> _payloads = [];

    private ITypeCompletionContext _context = default!;
    private ObjectType? _queryType;
    private ObjectFieldConfiguration _queryField = default!;
    private ObjectTypeConfiguration? _mutationConfig;

    public override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeConfiguration configuration,
        OperationType operationType)
    {
        _context ??= completionContext;

        switch (operationType)
        {
            case OperationType.Query:
                _queryType = (ObjectType)completionContext.Type;
                break;

            case OperationType.Mutation:
                _mutationConfig = (ObjectTypeConfiguration)configuration;
                break;
        }
    }

    public override void OnBeforeCompleteTypes()
    {
        if (_queryType is not null && _mutationConfig is not null)
        {
            var options = _context.DescriptorContext.GetMutationPayloadOptions();

            TypeReference queryType = TypeReference.Parse($"{_queryType.Name}!");

            _queryField = new ObjectFieldConfiguration(
                options.QueryFieldName ?? _defaultFieldName,
                type: queryType,
                resolver: ctx => new(ctx.GetQueryRoot<object>()));
            _queryField.Flags |= CoreFieldFlags.MutationQueryField;

            foreach (var field in _mutationConfig.Fields)
            {
                if (!field.IsIntrospectionField
                    && _context.TryGetType(field.Type!, out IType? returnType)
                    && returnType.NamedType() is ObjectType payloadType
                    && options.MutationPayloadPredicate.Invoke(payloadType))
                {
                    _payloads.Add(payloadType.Name);
                }
            }
        }
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        if (completionContext.Type is ObjectType objectType
            && configuration is ObjectTypeConfiguration objectTypeDef
            && _payloads.Contains(objectType.Name))
        {
            if (objectTypeDef.Fields.Any(t => t.Name.EqualsOrdinal(_queryField.Name)))
            {
                return;
            }

            objectTypeDef.Fields.Add(_queryField);
        }
    }
}
