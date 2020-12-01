using System;
using StrawberryShake.Configuration;
using StrawberryShake.Http;

namespace StrawberryShake
{
    public static class HttpOperationClientBuilderExtensions
    {
        public static IOperationClientBuilder AddHttpOperationPipeline(
            this IOperationClientBuilder builder,
            Action<IOperationPipelineBuilder<IHttpOperationContext>> configure) =>
            builder.AddOperationPipeline(sp =>
            {
                OperationPipelineBuilder<IHttpOperationContext> builder =
                    OperationPipelineBuilder<IHttpOperationContext>.New();
                configure(builder);
                return builder.Build(sp);
            });
    }
}
