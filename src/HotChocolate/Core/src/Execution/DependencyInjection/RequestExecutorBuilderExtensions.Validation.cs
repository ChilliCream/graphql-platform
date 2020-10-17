using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.Validation.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder ConfigureValidation(
            this IRequestExecutorBuilder builder,
            Action<IValidationBuilder> configure)
        {
            configure(builder.Services.AddValidation(builder.Name));
            return builder;
        }

        public static IRequestExecutorBuilder AddMaxComplexityRule(
            this IRequestExecutorBuilder builder,
            int maxAllowedComplexity) =>
            ConfigureValidation(builder, b => b.AddMaxComplexityRule(maxAllowedComplexity));

        public static IRequestExecutorBuilder AddMaxExecutionDepthRule(
            this IRequestExecutorBuilder builder,
            int maxAllowedExecutionDepth) =>
            ConfigureValidation(builder, b => b.AddMaxExecutionDepthRule(maxAllowedExecutionDepth));
    }
}
