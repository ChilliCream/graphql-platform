using System;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;

namespace Microsoft.Extensions.DependencyInjection;

public static class JsonRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddJsonSupport(this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.ConfigureSchema(sb => sb.AddSchemaDirective(new FromJsonSchemaDirective()));
        return builder;
    }
}
