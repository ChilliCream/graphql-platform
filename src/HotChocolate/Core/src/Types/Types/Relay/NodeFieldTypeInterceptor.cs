using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Introspection;
using Microsoft.Extensions.DependencyInjection;

#nullable enable

namespace HotChocolate.Types.Relay
{
    internal sealed class NodeFieldTypeInterceptor : TypeInterceptor
    {
        private const string _node = "node";
        private const string _id = "id";

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
                    _node);

                IIdSerializer serializer =
                    completionContext.Services.GetService<IIdSerializer>() ??
                        new IdSerializer();

                descriptor
                    .Argument(_id, a => a.Type<NonNullType<IdType>>())
                    .Type<NodeType>()
                    .Resolve(async ctx =>
                    {
                        var id = ctx.ArgumentValue<string>(_id);
                        IdValue deserializedId = serializer.Deserialize(id);

                        ctx.LocalContextData = ctx.LocalContextData
                            .SetItem(WellKnownContextData.Id, deserializedId.Value)
                            .SetItem(WellKnownContextData.Type, deserializedId.TypeName);

                        if (ctx.Schema.TryGetType(deserializedId.TypeName,
                                out ObjectType type)
                            && type.ContextData.TryGetValue(
                                RelayConstants.NodeResolverFactory,
                                out object? o)
                            && o is Func<IServiceProvider, INodeResolver> factory)
                        {
                            INodeResolver resolver = factory.Invoke(ctx.Services);
                            return await resolver.ResolveAsync(ctx, deserializedId.Value)
                                .ConfigureAwait(false);
                        }

                        return null;
                    });

                objectTypeDefinition.Fields.Insert(index, descriptor.CreateDefinition());
            }
        }
    }
}
