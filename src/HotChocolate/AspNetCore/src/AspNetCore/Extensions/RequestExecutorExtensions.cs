using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore;

internal static class RequestExecutorExtensions
{
    internal static T GetRequiredService<T>(
        this IRequestExecutor requestExecutor)
        where T : notnull =>
        requestExecutor.Schema.Services.GetRequiredService<T>();

    internal static IErrorHandler GetErrorHandler(
        this IRequestExecutor requestExecutor) =>
        requestExecutor.GetRequiredService<IErrorHandler>();

    internal static IHttpRequestInterceptor GetRequestInterceptor(
        this IRequestExecutor requestExecutor) =>
        requestExecutor.GetRequiredService<IHttpRequestInterceptor>();
}
