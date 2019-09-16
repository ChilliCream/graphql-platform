using StrawberryShake.Http.Pipelines;

namespace StrawberryShake.Http
{
    public static class HttpOperationExecutorBuilderExtensions
    {
        public static IHttpOperationExecutorBuilder Use<T>(
            this IHttpOperationExecutorBuilder builder)
        {
            return builder.Use(ClassMiddlewareFactory.Create(typeof(T)));
        }
    }
}
