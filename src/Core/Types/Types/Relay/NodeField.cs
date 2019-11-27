using System;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

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

            IIdSerializer _serializer = null;

            descriptor
                .Argument(_id, a => a.Type<NonNullType<IdType>>())
                .Type<NodeType>()
                .Resolver(async ctx =>
                {
                    IServiceProvider services = ctx.Service<IServiceProvider>();

                    if (_serializer is null)
                    {
                        _serializer =
                            services.GetService(typeof(IIdSerializer)) is IIdSerializer s
                                ? s
                                : new IdSerializer();
                    }

                    var id = ctx.Argument<string>(_id);
                    IdValue deserializedId = _serializer.Deserialize(id);

                    ctx.LocalContextData = ctx.LocalContextData
                        .SetItem(WellKnownContextData.Id, deserializedId.Value)
                        .SetItem(WellKnownContextData.Type, deserializedId.TypeName);

                    if (ctx.Schema.TryGetType(deserializedId.TypeName,
                        out ObjectType type)
                        && type.ContextData.TryGetValue(
                            RelayConstants.NodeResolverFactory,
                            out var o)
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
