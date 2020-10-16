using System;
using System.Collections.Generic;
using System.Linq;
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
            DefinitionBase definition,
            IDictionary<string, object> contextData)
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
}
