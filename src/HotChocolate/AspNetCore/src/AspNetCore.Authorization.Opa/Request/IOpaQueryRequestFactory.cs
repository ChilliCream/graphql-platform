using HotChocolate.Authorization;

namespace HotChocolate.AspNetCore.Authorization;

public interface IOpaQueryRequestFactory
{
    OpaQueryRequest CreateRequest(
        AuthorizationContext context,
        AuthorizeDirective directive);
}
