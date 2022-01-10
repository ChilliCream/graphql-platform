namespace HotChocolate.AspNetCore.Authorization;

public class DefaultOpaDecision : IOpaDecision
{
    public AuthorizeResult Map(ResponseBase? response) => response switch
    {
        QueryResponse { Result: true } => AuthorizeResult.Allowed,
        NoDefaultPolicy => AuthorizeResult.NoDefaultPolicy,
        PolicyNotFound => AuthorizeResult.PolicyNotFound,
        _ => AuthorizeResult.NotAllowed
    };
}
