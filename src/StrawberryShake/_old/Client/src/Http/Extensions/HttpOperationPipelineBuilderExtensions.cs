using StrawberryShake.Http.Pipelines;
using HttpPipelineBuilder = StrawberryShake.IOperationPipelineBuilder<StrawberryShake.Http.IHttpOperationContext>;

namespace StrawberryShake
{
    public static class HttpOperationPipelineBuilderExtensions
    {
        public static HttpPipelineBuilder UseHttpDefaultPipeline(
            this HttpPipelineBuilder builder)
        {
            return builder
                .Use<ExceptionMiddleware>()
                .Use<CreateStandardRequestMiddleware>()
                .Use<SendHttpRequestMiddleware>()
                .Use<ParseSingleResultMiddleware>();
        }
    }
}
