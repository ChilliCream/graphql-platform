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
