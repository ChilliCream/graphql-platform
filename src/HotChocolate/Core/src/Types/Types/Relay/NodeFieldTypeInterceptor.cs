using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Introspection;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Types.WellKnownContextData;

#nullable enable

namespace HotChocolate.Types.Relay
{
    internal sealed class NodeFieldTypeInterceptor : TypeInterceptor
    {
        private static NameString Node { get; } = "node";
        private static NameString Id { get; } = "id";

        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if ((completionContext.IsQueryType ?? false) &&
                definition is ObjectTypeDefinition objectTypeDefinition)
            {
                ObjectFieldDefinition typeNameField = objectTypeDefinition.Fields.First(
                    t => t.Name.Equals(IntrospectionFields.TypeName) && t.IsIntrospectionField);
                var index = objectTypeDefinition.Fields.IndexOf(typeNameField) + 1;

                var descriptor = ObjectFieldDescriptor.New(
                    completionContext.DescriptorContext,
                    Node);

                IIdSerializer serializer =
                    completionContext.Services.GetService<IIdSerializer>() ??
                        new IdSerializer();

                descriptor
                    .Argument(Id, a => a.Type<NonNullType<IdType>>().ID())
                    .Type<NodeType>()
                    .Resolve(async ctx =>
                    {
                        StringValueNode id = ctx.ArgumentLiteral<StringValueNode>(Id);
                        IdValue deserializedId = serializer.Deserialize(id.Value);

                        ctx.SetLocalValue(NodeId, id.Value);
                        ctx.SetLocalValue(InternalId, deserializedId.Value);
                        ctx.SetLocalValue(InternalType, deserializedId.TypeName);
                        ctx.SetLocalValue(WellKnownContextData.IdValue, deserializedId);

                        if (ctx.Schema.TryGetType(deserializedId.TypeName, out ObjectType type) &&
                            type.ContextData.TryGetValue(NodeResolver, out object? o) &&
                            o is FieldResolverDelegate resolver)
                        {
                            return await resolver.Invoke(ctx).ConfigureAwait(false);
                        }

                        return null;
                    });

                objectTypeDefinition.Fields.Insert(index, descriptor.CreateDefinition());
            }
        }
    }

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
            IReadOnlyCollection<ITypeCompletionContext> discoveryContexts)
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
                .Resolver(ctx => ctx.GetQueryRoot<object>());

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

            public bool IsQuery => Context.IsMutationType ?? false;

            public bool IsMutation => Context.IsMutationType ?? false;
        }
    }

    internal static class RelayHelper
    {
        public static RelayOptions GetRelayOptions(
            this IDescriptorContext context)
        {
            if (context.ContextData.TryGetValue(typeof(RelayOptions).FullName!, out object? o) &&
                o is RelayOptions casted)
            {
                return casted;
            }

            return new RelayOptions();
        }

        public static ISchemaBuilder SetRelayOptions(
            this ISchemaBuilder schemaBuilder,
            RelayOptions options) =>
            schemaBuilder.SetContextData(typeof(RelayOptions).FullName!, options);
    }
}
