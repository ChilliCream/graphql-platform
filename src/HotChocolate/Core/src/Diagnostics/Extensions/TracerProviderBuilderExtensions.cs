using System;
using HotChocolate.Diagnostics;

namespace OpenTelemetry.Trace;

public static class TracerProviderBuilderExtensions
{
    public static TracerProviderBuilder AddHotChocolateInstrumentation(
        this TracerProviderBuilder builder,
        Action<HotChocolateInstrumentationOptions>? configure = null)
    {
        if (builder is IDeferredTracerProviderBuilder deferredTracerProviderBuilder)
        {   
            return deferredTracerProviderBuilder.Configure((sp, b) =>
            {
                
            });
        }

        builder.AddSource(HotChocolateActivitySource.GetName());
    }
}