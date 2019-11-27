using System;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Relay
{
    internal sealed class NodeField
         : ObjectField
    {
        internal NodeField(IDescriptorContext context)
           : base(CreateDefinition(context))
        {
        }

        private static ObjectFieldDefinition CreateDefinition(
            IDescriptorContext context)
        {
            var descriptor = ObjectFieldDescriptor.New(context, "node");

            IIdSerializer _serializer = null;

            descriptor
                .Argument("id", a => a.Type<NonNullType<IdType>>())
                .Type<NodeType>()
                .Resolver(async ctx =>
                {
                    if (_serializer is null)
                    {
                        var services = ctx.Service<IServiceProvider>();
                        _serializer = services.GetService(typeof(IIdSerializer)) is IIdSerializer s
                            ? s
                            : new IdSerializer();
                    }

                    string id = ctx.Argument<string>("id");
                    IdValue deserializedId = _serializer.Deserialize(id);

                    if (ctx.Schema.TryGetType(deserializedId.TypeName,
                        out ObjectType type)
                        && type.ContextData.TryGetValue(
                            RelayConstants.NodeResolverFactory,
                            out object o)
                        && o is Func<IServiceProvider, INodeResolver> factory)
                    {
                        INodeResolver resolver =
                            factory.Invoke(ctx.Service<IServiceProvider>());

                        return await resolver.ResolveAsync(
                            ctx, deserializedId.Value)
                            .ConfigureAwait(false);
                    }

                    return null;
                });

            return descriptor.CreateDefinition();
        }
    }
}
