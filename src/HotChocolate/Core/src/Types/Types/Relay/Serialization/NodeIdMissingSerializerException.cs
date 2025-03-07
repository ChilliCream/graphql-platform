#nullable enable
namespace HotChocolate.Types.Relay;

#pragma warning disable RCS1194
public sealed class NodeIdMissingSerializerException(string typeName)
    : GraphQLException(ErrorBuilder.New()
        .SetMessage("No serializer registered for type `{0}`.", typeName)
        .SetExtension(nameof(typeName), typeName)
        .Build());
#pragma warning restore RCS1194
