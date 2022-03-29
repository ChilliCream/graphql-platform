namespace HotChocolate.AspNetCore.Authorization;

public class DefaultOpaDecision : IOpaDecision
{
    public AuthorizeResult Map(ResponseBase? response) => response switch
    {
        QueryResponse<bool?> { Result: true } => AuthorizeResult.Allowed,
        PolicyNotFound => AuthorizeResult.PolicyNotFound,
        NoDefaultPolicy => AuthorizeResult.NoDefaultPolicy,
        _ => AuthorizeResult.NotAllowed
    };
}
