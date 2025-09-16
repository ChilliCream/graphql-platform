using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Properties;

namespace HotChocolate.Fusion.Text.Json;

internal static class ThrowHelper
{
    [DoesNotReturn]
    public static void ThrowFormatException()
    {
        throw new FormatException { Source = FusionExecutionResources.Rethrowable };
    }
}
