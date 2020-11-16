using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore
{
    internal static class RequestExecutorExtensions
    {
        public static T GetRequiredService<T>(
            this IRequestExecutor requestExecutor)
            where T : notnull =>
            requestExecutor.Services.GetRequiredService<T>();

        public static IErrorHandler GetErrorHandler(
            this IRequestExecutor requestExecutor) =>
            requestExecutor.GetRequiredService<IErrorHandler>();

        public static IHttpRequestInterceptor GetRequestInterceptor(
            this IRequestExecutor requestExecutor) =>
            requestExecutor.GetRequiredService<IHttpRequestInterceptor>();
    }
}
