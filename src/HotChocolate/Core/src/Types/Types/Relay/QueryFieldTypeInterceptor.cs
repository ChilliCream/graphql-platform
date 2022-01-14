using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Relay;

internal sealed class QueryFieldTypeInterceptor : TypeInterceptor
{
    private const string _defaultFieldName = "query";
    private readonly HashSet<NameString> _payloads = new();

    private ITypeCompletionContext _context = default!;
    private ObjectType? _queryType;
    private ObjectFieldDefinition _queryField = default!;
    private ObjectTypeDefinition? _mutationDefinition;

    internal override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition,
        OperationType operationType,
        IDictionary<string, object?> contextData)
    {
        _context ??= completionContext;

        switch (operationType)
        {
            case OperationType.Query:
                _queryType = (ObjectType)completionContext.Type;
                break;

            case OperationType.Mutation:
                _mutationDefinition = (ObjectTypeDefinition)definition;
                break;
        }
    }

    public override void OnBeforeCompleteTypes()
    {
        if (_queryType is not null && _mutationDefinition is not null)
        {
            MutationPayloadOptions options = _context.DescriptorContext.GetMutationPayloadOptions();

            ITypeReference queryType = TypeReference.Parse($"{_queryType.Name}!");

            _queryField= new ObjectFieldDefinition(
                options.QueryFieldName ?? _defaultFieldName,
                type: queryType,
                resolver: ctx => new(ctx.GetQueryRoot<object>()));

            foreach (ObjectFieldDefinition field in _mutationDefinition.Fields)
            {
                if (!field.IsIntrospectionField &&
                    _context.TryGetType(field.Type!, out IType? returnType) &&
                    returnType.NamedType() is ObjectType payloadType &&
                    options.MutationPayloadPredicate.Invoke(payloadType))
                {
                    _payloads.Add(payloadType.Name);
                }
            }
        }
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        if (completionContext.Type is ObjectType objectType &&
            definition is ObjectTypeDefinition objectTypeDef &&
            _payloads.Contains(objectType.Name))
        {
            if (objectTypeDef.Fields.Any(t => t.Name.Equals(_queryField.Name)))
            {
                return;
            }

            objectTypeDef.Fields.Add(_queryField);
        }
    }
}
