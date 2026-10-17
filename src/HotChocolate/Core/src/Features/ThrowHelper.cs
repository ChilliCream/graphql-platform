namespace HotChocolate.Features;

internal static class ThrowHelper
{
    public static NullReferenceException FeatureKeyIsNull() => new();
}
