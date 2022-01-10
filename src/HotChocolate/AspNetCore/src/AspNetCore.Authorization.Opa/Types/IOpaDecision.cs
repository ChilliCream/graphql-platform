namespace HotChocolate.AspNetCore.Authorization;

public interface IOpaDecision
{
    AuthorizeResult Map(ResponseBase? response);
}
