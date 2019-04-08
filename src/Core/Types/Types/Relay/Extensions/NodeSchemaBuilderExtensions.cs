using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using HotChocolate.Utilities;

namespace HotChocolate
{
    public static class NodeSchemaBuilderExtensions
    {
        private static readonly IdSerializer _idSerializer = new IdSerializer();

        public static IObjectFieldDescriptor NodeField(
            this IObjectTypeDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Field("node")
                .Type<NodeType>()
                .Argument("id", a => a.Type<NonNullType<IdType>>())
                .Resolver(async ctx =>
                {
                    string id = ctx.Argument<string>("id");
                    IdValue deserializedId = _idSerializer.Deserialize(id);

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
        }
    }
}
