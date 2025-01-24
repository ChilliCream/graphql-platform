#nullable enable
namespace HotChocolate.Types.Relay;

#pragma warning disable RCS1194
public sealed class NodeIdInvalidFormatException(object originalValue)
    : GraphQLException(ErrorBuilder.New()
        .SetMessage("The node ID string has an invalid format.")
        .SetExtension(nameof(originalValue), originalValue.ToString())
        .Build());
#pragma warning restore RCS1194
