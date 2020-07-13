using System;
using HotChocolate.Configuration;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring an <see cref="IRequestExecutorBuilder"/>
    /// </summary>
    public static partial class SchemaRequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder SetOptions(
            this IRequestExecutorBuilder builder,
            IReadOnlySchemaOptions options)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return builder.ConfigureSchema(b => b.SetOptions(options));
        }

        public static IRequestExecutorBuilder ModifyOptions(
            this IRequestExecutorBuilder builder,
            Action<ISchemaOptions> configure)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.ConfigureSchema(b => b.ModifyOptions(configure));
        }
        
        public static IRequestExecutorBuilder SetContextData(
            this IRequestExecutorBuilder builder,
            string key,
            object? value)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return builder.ConfigureSchema(b => b.SetContextData(key, value));
        }
    }
}