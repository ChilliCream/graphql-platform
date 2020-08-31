using System;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Relay
{
    internal sealed class NodeField
         : ObjectField
    {
        private const string _node = "node";
        private const string _id = "id";

        internal NodeField(IDescriptorContext context)
           : base(CreateDefinition(context))
        {
        }

        private static ObjectFieldDefinition CreateDefinition(
            IDescriptorContext context)
        {
            var descriptor = ObjectFieldDescriptor.New(context, _node);

            IIdSerializer? serializer = null;

            descriptor
                .Argument(_id, a => a.Type<NonNullType<IdType>>())
                .Type<NodeType>()
                .Resolve(async ctx =>
                {
                    IServiceProvider services = ctx.Service<IServiceProvider>();

                    serializer ??= services.GetService(typeof(IIdSerializer)) is IIdSerializer s
                        ? s
                        : new IdSerializer();

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
                        INodeResolver resolver = factory.Invoke(services);
                        return await resolver.ResolveAsync(ctx, deserializedId.Value)
                            .ConfigureAwait(false);
                    }

                    return null;
                });

            return descriptor.CreateDefinition();
        }
    }
}
