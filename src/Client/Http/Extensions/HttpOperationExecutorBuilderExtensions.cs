using StrawberryShake.Http.Pipelines;

namespace StrawberryShake.Http
{
    public static class HttpOperationExecutorBuilderExtensions
    {
        public static IHttpPipelineBuilder Use<T>(
            this IHttpPipelineBuilder builder)
        {
            return builder.Use(ClassMiddlewareFactory.Create(typeof(T)));
        }
    }
}
