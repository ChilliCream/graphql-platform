using HotChocolate.Resolvers;

namespace HotChocolate.Types.Relay
{
    public static class IdMiddleware
    {
        public static ResultConverterDelegate Create()
            => (context, result) =>
                result is not null
                    ? context.Service<IIdSerializer>().Serialize(
                        context.Schema.Name,
                        context.ObjectType.Name,
                        result)
                    : null;
    }
}
