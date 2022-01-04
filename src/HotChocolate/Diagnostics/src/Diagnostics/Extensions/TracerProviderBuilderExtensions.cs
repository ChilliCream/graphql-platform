using HotChocolate.Diagnostics;

namespace OpenTelemetry.Trace;

public static class TracerProviderBuilderExtensions
{
    public static TracerProviderBuilder AddHotChocolateInstrumentation(
        this TracerProviderBuilder builder)
    {
        builder.AddSource(HotChocolateActivitySource.GetName());
        return builder;
    }
}
