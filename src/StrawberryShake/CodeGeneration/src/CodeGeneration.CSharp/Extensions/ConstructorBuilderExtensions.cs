using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp;

internal static class ConstructorBuilderExtensions
{
    public static ConstructorBuilder SetPublic(this ConstructorBuilder builder)
    {
        return builder.SetAccessModifier(AccessModifier.Public);
    }

    public static ConstructorBuilder SetPrivate(this ConstructorBuilder builder)
    {
        return builder.SetAccessModifier(AccessModifier.Private);
    }

    public static ConstructorBuilder SetInternal(this ConstructorBuilder builder)
    {
        return builder.SetAccessModifier(AccessModifier.Internal);
    }

    public static ConstructorBuilder SetProtected(this ConstructorBuilder builder)
    {
        return builder.SetAccessModifier(AccessModifier.Protected);
    }

    public static ConstructorBuilder AddParameter(
        this ConstructorBuilder builder,
        string name,
        Action<ParameterBuilder> configure)
    {
        var parameterBuilder = ParameterBuilder.New().SetName(name);
        configure(parameterBuilder);
        builder.AddParameter(parameterBuilder);
        return builder;
    }

    public static ParameterBuilder AddParameter(
        this ConstructorBuilder builder,
        string? name = null)
    {
        var parameterBuilder = ParameterBuilder.New();

        if (name is not null)
        {
            parameterBuilder.SetName(name);
        }

        builder.AddParameter(parameterBuilder);
        return parameterBuilder;
    }

    public static CodeBlockBuilder AddBody(this ConstructorBuilder method)
    {
        var code = CodeBlockBuilder.New();
        method.AddCode(code);
        return code;
    }
}
