using HotChocolate.Types.Properties;

namespace HotChocolate.Types;

internal static class ThrowHelper
{
    public static SchemaException MessageWasNotDefinedOnError(IType type, Type runtimeType)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorResources.ThrowHelper_ErrorObjectType_MessageWasNotDefinedOnError,
                type.GetType().FullName,
                runtimeType.FullName)
            .BuildException();
}
