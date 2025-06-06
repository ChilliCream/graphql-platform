namespace HotChocolate.Execution;

public record RequestMiddlewareConfiguration(
    RequestMiddleware Middleware,
    string? Key = null);
