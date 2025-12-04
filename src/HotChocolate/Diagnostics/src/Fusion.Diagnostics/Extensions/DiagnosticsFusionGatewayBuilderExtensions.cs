using System.Text;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Diagnostics.Listeners;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.DependencyInjection;

public static class DiagnosticsFusionGatewayBuilderExtensions
{
    public static IFusionGatewayBuilder AddInstrumentation(
        this IFusionGatewayBuilder builder,
        Action<InstrumentationOptions>? options = null)
        => AddInstrumentation(builder, (_, opt) => options?.Invoke(opt));

    public static IFusionGatewayBuilder AddInstrumentation(
        this IFusionGatewayBuilder builder,
        Action<IServiceProvider, InstrumentationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        builder.Services.TryAddSingleton(
            sp =>
            {
                var optionInst = new InstrumentationOptions();
                options(sp, optionInst);
                return optionInst;
            });

        builder.Services.TryAddSingleton<InternalActivityEnricher>();

        builder.AddDiagnosticEventListener(
            sp => new ActivityFusionExecutionDiagnosticEventListener(
                sp.GetService<FusionActivityEnricher>() ??
                    sp.GetRequiredService<InternalActivityEnricher>(),
                sp.GetRequiredService<InstrumentationOptions>()));

        builder.AddDiagnosticEventListener(
            sp => new ActivityServerDiagnosticListener(
                sp.GetService<FusionActivityEnricher>() ??
                    sp.GetRequiredService<InternalActivityEnricher>(),
                sp.GetRequiredService<InstrumentationOptions>()));

        return builder;
    }

    private sealed class InternalActivityEnricher : FusionActivityEnricher
    {
        public InternalActivityEnricher(
            ObjectPool<StringBuilder> stringBuilderPool,
            InstrumentationOptions options)
            : base(stringBuilderPool, options)
        {
        }
    }
}
