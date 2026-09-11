namespace HotChocolate.Types.BatchResolvers;

internal static class ThrowHelper
{
    public static InvalidOperationException DeclarationNotApplicable(string reason)
        => new($"The declaration is not applicable: {reason}");

    public static ArgumentOutOfRangeException UnknownDeclarationStyle(DeclarationStyle style)
        => new(nameof(style), style, "Unknown declaration style.");
}
