using HotChocolate.Authorization;
using HotChocolate.Resolvers;

namespace HotChocolate.AspNetCore.Authorization;

public interface IOpaQueryRequestFactory
{
    QueryRequest CreateRequest(IMiddlewareContext context, AuthorizeDirective directive);
}
