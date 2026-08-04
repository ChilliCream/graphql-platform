using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

public sealed class HttpPostMiddleware(
    HttpRequestDelegate next,
    HttpRequestExecutorProxy executor,
    GraphQLServerOptions baseOptions)
    : HttpPostMiddlewareBase(next, executor, baseOptions);
