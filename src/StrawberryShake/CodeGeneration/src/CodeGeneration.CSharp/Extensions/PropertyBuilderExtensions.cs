using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp;

internal static class PropertyBuilderExtensions
{
    public static PropertyBuilder SetPublic(this PropertyBuilder builder)
    {
        return builder.SetAccessModifier(AccessModifier.Public);
    }

    public static PropertyBuilder SetPrivate(this PropertyBuilder builder)
    {
        return builder.SetAccessModifier(AccessModifier.Private);
    }

    public static PropertyBuilder SetInternal(this PropertyBuilder builder)
    {
        return builder.SetAccessModifier(AccessModifier.Internal);
    }

    public static PropertyBuilder SetProtected(this PropertyBuilder builder)
    {
        return builder.SetAccessModifier(AccessModifier.Protected);
    }
}
