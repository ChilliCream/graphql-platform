namespace HotChocolate.Execution.Relay;

public sealed class NodeIdInvalidValueException(string typeName, object value)
    : GraphQLException(ErrorBuilder.New()
        .SetMessage(
            $"The value of type `{value.GetType().FullName}` could not be formatted "
            + $"into an ID for the type `{typeName}`.")
        .SetExtension("originalValue", value.ToString())
        .Build());
