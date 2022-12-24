using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Relay;

public static class IdMiddleware
{
    public static ResultFormatterDefinition Create()
        => new((context, result) =>
                result is not null
                    ? context.Service<IIdSerializer>().Serialize(
                        context.Schema.Name,
                        context.ObjectType.Name,
                        result)
                    : null,
            key: WellKnownMiddleware.GlobalId,
            isRepeatable: false);
}
