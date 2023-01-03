using HotChocolate.Authorization;

namespace HotChocolate.AspNetCore.Authorization;

public static class PolicyResultContextExtensions
{
    public static OpaAuthResult<T> Allowed<T>(this PolicyResultContext<T> context)
        => new(AuthorizeResult.Allowed, context.Result);

    public static OpaAuthResult<T> NotAllowed<T>(this PolicyResultContext<T> context)
        => new(AuthorizeResult.NotAllowed, context.Result);

    public static OpaAuthResult<T> NotAuthenticated<T>(this PolicyResultContext<T> context)
        => new(AuthorizeResult.NotAuthenticated, context.Result);

    public static OpaAuthResult<T> PolicyNotFound<T>(this PolicyResultContext<T> context)
        => new(AuthorizeResult.PolicyNotFound, context.Result);

    public static OpaAuthResult<T> NoDefaultPolicy<T>(this PolicyResultContext<T> context)
        => new(AuthorizeResult.NoDefaultPolicy, context.Result);
}
