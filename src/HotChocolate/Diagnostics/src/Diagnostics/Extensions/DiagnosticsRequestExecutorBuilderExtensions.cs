using System;
using System.Text;
using HotChocolate.Diagnostics;
using HotChocolate.Diagnostics.Listeners;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.DependencyInjection;

public static class DiagnosticsRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddInstrumentation(
        this IRequestExecutorBuilder builder,
        Action<InstrumentationOptions>? options = default)
        => AddInstrumentation(builder, (_, opt) => options?.Invoke(opt));

    public static IRequestExecutorBuilder AddInstrumentation(
        this IRequestExecutorBuilder builder,
        Action<IServiceProvider, InstrumentationOptions> options)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        builder.Services.TryAddSingleton(
            sp =>
            {
                var optionInst = new InstrumentationOptions();
                options(sp, optionInst);
                return optionInst;
            });

        builder.Services.TryAddSingleton<InternalActivityEnricher>();

        builder.AddDiagnosticEventListener(
            sp => new ActivityExecutionDiagnosticListener(
                sp.GetService<ActivityEnricher>() ??
                    sp.GetRequiredService<InternalActivityEnricher>(),
                sp.GetRequiredService<InstrumentationOptions>()));

        builder.AddDiagnosticEventListener(
            sp => new ActivityServerDiagnosticListener(
                sp.GetService<ActivityEnricher>() ??
                    sp.GetRequiredService<InternalActivityEnricher>(),
                sp.GetRequiredService<InstrumentationOptions>()));

        return builder;
    }

    private sealed class InternalActivityEnricher : ActivityEnricher
    {
        public InternalActivityEnricher(
            ObjectPool<StringBuilder> stringBuilderPoolPool,
            InstrumentationOptions options)
            : base(stringBuilderPoolPool, options)
        {
        }
    }
}
