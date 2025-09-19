using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp;

internal static class LambdaBuilderExtensions
{
    public static LambdaBuilder SetCode(this LambdaBuilder builder, string code)
    {
        return builder.SetCode(CodeInlineBuilder.From(code));
    }
}
