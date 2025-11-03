using HotChocolate.Features;

namespace HotChocolate.Types;

internal static class ErrorContextDataExtensions
{
    public static ErrorTypeFeature MarkAsError(this ObjectTypeConfiguration featureProvider)
        => featureProvider.Features.GetOrSet<ErrorTypeFeature>();

    public static bool IsError(this ObjectTypeConfiguration featureProvider)
        => featureProvider.Features.Get<ErrorTypeFeature>() is not null;
}
