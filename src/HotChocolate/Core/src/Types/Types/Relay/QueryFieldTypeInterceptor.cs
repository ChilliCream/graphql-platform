using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Relay
{
    internal sealed class QueryFieldTypeInterceptor : TypeInterceptor
    {
        private const string _defaultFieldName = "query";
        private readonly Dictionary<IType, TypeInfo> _types = new();

        public override bool TriggerAggregations => true;

        public override void OnAfterCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (completionContext.Type is ObjectType objectType &&
                definition is ObjectTypeDefinition objectTypeDefinition)
            {
                var typeInfo = new TypeInfo(completionContext, objectType, objectTypeDefinition);
                _types.Add(typeInfo.Type, typeInfo);
            }
        }

        public override void OnTypesCompletedName(
            IReadOnlyCollection<ITypeCompletionContext> completionContexts)
        {
            TypeInfo query = _types.Values.FirstOrDefault(t => t.IsQuery);
            TypeInfo mutation = _types.Values.FirstOrDefault(t => t.IsMutation);

            if (query is { Context: not null } && mutation is { Context: not null })
            {
                RelayOptions options = query.Context.DescriptorContext.GetRelayOptions();
                options.QueryFieldName ??= _defaultFieldName;

                foreach (var field in mutation.Definition.Fields)
                {
                    if (mutation.Context.TryGetType(field.Type, out IType type) &&
                        type.NamedType() is ObjectType objectType &&
                        objectType.Name.Value.EndsWith("Payload") &&
                        _types.TryGetValue(objectType, out TypeInfo payload))
                    {
                        TryAddQueryField(payload, query, options.QueryFieldName.Value);
                    }
                }
            }
        }

        private void TryAddQueryField(TypeInfo payload, TypeInfo query, NameString queryFieldName)
        {
            if (payload.Definition.Fields.Any(t => t.Name.Equals(queryFieldName)))
            {
                return;
            }

            var descriptor = ObjectFieldDescriptor.New(
                payload.Context.DescriptorContext,
                queryFieldName);

            descriptor
                .Type(new NonNullType(query.Type))
                .Resolver(ctx =>
                {
                    return ctx.GetQueryRoot<object>();
                });

            payload.Definition.Fields.Add(descriptor.CreateDefinition());
        }

        private readonly struct TypeInfo
        {
            public TypeInfo(
                ITypeCompletionContext context,
                ObjectType type,
                ObjectTypeDefinition definition)
            {
                Context = context;
                Type = type;
                Definition = definition;
            }

            public ITypeCompletionContext Context { get; }

            public ObjectType Type { get; }

            public ObjectTypeDefinition Definition { get; }

            public bool IsQuery => Context.IsQueryType ?? false;

            public bool IsMutation => Context.IsMutationType ?? false;
        }
    }
}
