#nullable enable
namespace HotChocolate.Types.Relay;

public sealed class NodeIdInvalidFormatException(object originalValue)
    : GraphQLException(ErrorBuilder.New()
        .SetMessage("The internal ID could not be formatted.")
        .SetExtension(nameof(originalValue), originalValue.ToString())
        .Build());
