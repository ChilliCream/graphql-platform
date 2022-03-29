namespace HotChocolate.AspNetCore.Authorization;

public static class PolicyResultContextExtensions
{
    public static OpaAuthzResult<T> Allowed<T>(this PolicyResultContext<T> context) =>
        new(AuthorizeResult.Allowed, context.Result);

    public static OpaAuthzResult<T> NotAllowed<T>(this PolicyResultContext<T> context) =>
        new(AuthorizeResult.NotAllowed, context.Result);

    public static OpaAuthzResult<T> NotAuthenticated<T>(this PolicyResultContext<T> context) =>
        new(AuthorizeResult.NotAuthenticated, context.Result);

    public static OpaAuthzResult<T> PolicyNotFound<T>(this PolicyResultContext<T> context) =>
        new(AuthorizeResult.PolicyNotFound, context.Result);

    public static OpaAuthzResult<T> NoDefaultPolicy<T>(this PolicyResultContext<T> context) =>
        new(AuthorizeResult.NoDefaultPolicy, context.Result);
}
