namespace HotChocolate.AspNetCore.Authorization;

public interface IOpaAuthzResult<out T> : IOpaAuthzResult
{
    T? Payload { get; }
}

public interface IOpaAuthzResult
{
    AuthorizeResult Result { get; }
}
