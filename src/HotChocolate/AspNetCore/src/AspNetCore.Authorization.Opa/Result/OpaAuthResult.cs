using HotChocolate.Authorization;

namespace HotChocolate.AspNetCore.Authorization;

public class OpaAuthResult<T> : IOpaAuthzResult<T>
{
    public OpaAuthResult(AuthorizeResult result, T? payload)
    {
        Result = result;
        Payload = payload;
    }

    public AuthorizeResult Result { get; }
    public T? Payload { get; }
}
