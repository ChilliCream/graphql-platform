using System;
using HotChocolate.Authorization.Properties;

namespace HotChocolate.Authorization;

internal static class ThrowHelper
{
    public static InvalidOperationException StateNotInitialized()
        => new(AuthCoreResources.ThrowHelper_StateNotInitialized);

    public static InvalidOperationException UnauthorizedType(
        FieldCoordinate fieldCoordinate)
        => new(string.Format(AuthCoreResources.ThrowHelper_UnauthorizedType, fieldCoordinate));

    public static InvalidOperationException UnableToResolveTypeReg()
        => new(AuthCoreResources.ThrowHelper_UnableToResolveTypeReg);
}
