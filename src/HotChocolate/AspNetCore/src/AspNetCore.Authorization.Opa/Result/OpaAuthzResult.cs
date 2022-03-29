namespace HotChocolate.AspNetCore.Authorization;

public class OpaAuthzResult<T> : IOpaAuthzResult<T>
{
    public OpaAuthzResult(AuthorizeResult result, T? payload)
    {
        Result = result;
        Payload = payload;
    }
    public AuthorizeResult Result { get; }
    public T? Payload { get; }
}

public static class OpaAuthzResult
{
    public static OpaAuthzResult<T> Allowed<T>(this PolicyResultContext<T> context) =>
        new(AuthorizeResult.Allowed, context.Result);

    public static OpaAuthzResult<T> NotAllowed<T>(this PolicyResultContext<T> context) =>
        new(AuthorizeResult.NotAllowed, default);

    public static OpaAuthzResult<T> PolicyNotFound<T>(this PolicyResultContext<T> context) =>
        new(AuthorizeResult.PolicyNotFound, default);

    public static OpaAuthzResult<T> NoDefaultPolicy<T>(this PolicyResultContext<T> context) =>
        new(AuthorizeResult.NoDefaultPolicy, default);
}
