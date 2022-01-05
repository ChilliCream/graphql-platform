namespace HotChocolate.AspNetCore.Authorization;

public sealed class OpaDecision : IOpaDecision
{
    public AuthorizeResult Map(QueryResponse? response)
    {
        if (response is not null && response.Result)
        {
            return AuthorizeResult.Allowed;
        }
        return AuthorizeResult.NotAllowed;
    }
}
