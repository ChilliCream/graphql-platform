#nullable enable
namespace HotChocolate.Types.Relay;

public sealed class NodeIdInvalidFormatException(object originalValue)
    : GraphQLException(ErrorBuilder.New()
        .SetMessage("The node ID string has an invalid format.")
        .SetExtension(nameof(originalValue), originalValue.ToString())
        .Build());
