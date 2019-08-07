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

        public override bool IsIntrospectionField { get; } = true;

        private static ObjectFieldDefinition CreateDefinition(
            IDescriptorContext context)
        {
            var descriptor = ObjectFieldDescriptor
                .New(context, "node");

            IIdSerializer _serializer = null;

            descriptor
                .Argument("id", a => a.Type<NonNullType<IdType>>())
                .Type<NonNullType<NodeType>>()
                .Resolver(async ctx =>
                {
                    if (_serializer is null)
                    {
                        _serializer = ctx.Service<IIdSerializer>();
                        if (_serializer == null)
                        {
                            // TODO : resources
                            throw new InvalidOperationException(
                                "The ID serializer was not registered as a service.");
                        }
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
