using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate
{
    public static class AggregationDirectiveRequestExecutorBuilder
    {
        public static IRequestExecutorBuilder AddAggregationDirectives(
            this IRequestExecutorBuilder executorBuilder) =>
            executorBuilder.ConfigureSchema(x => x.AddAggregationDirectives());
    }
}
