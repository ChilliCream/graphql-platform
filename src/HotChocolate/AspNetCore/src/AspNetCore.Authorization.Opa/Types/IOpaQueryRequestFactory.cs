using HotChocolate.Authorization;

namespace HotChocolate.AspNetCore.Authorization;

public interface IOpaQueryRequestFactory
{
    QueryRequest CreateRequest(AuthorizationContext context, AuthorizeDirective directive);
}
