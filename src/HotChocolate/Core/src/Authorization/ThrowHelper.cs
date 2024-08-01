using HotChocolate.Authorization.Properties;

namespace HotChocolate.Authorization;

internal static class ThrowHelper
{
    public static InvalidOperationException StateNotInitialized()
        => new(AuthCoreResources.ThrowHelper_StateNotInitialized);

    public static InvalidOperationException UnauthorizedType(
        SchemaCoordinate schemaCoordinate)
        => new(string.Format(AuthCoreResources.ThrowHelper_UnauthorizedType, schemaCoordinate));

    public static InvalidOperationException UnableToResolveTypeReg()
        => new(AuthCoreResources.ThrowHelper_UnableToResolveTypeReg);
}
