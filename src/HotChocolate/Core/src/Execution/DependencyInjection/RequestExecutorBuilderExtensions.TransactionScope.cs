using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Pipeline;
using HotChocolate.Validation;
using HotChocolate.Validation.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder AddDefaultTransactionScopeHandler<T>(
            this IRequestExecutorBuilder builder)
            where T : ITransactionScopeHandler, new()
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return ConfigureValidation(builder, b => b.TryAddValidationVisitor<T>());
        }
    }
}
