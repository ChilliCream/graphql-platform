using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    internal static class CodeBlockBuilderExtensions
    {
        public static CodeBlockBuilder AddLine(this CodeBlockBuilder builder, string code)
        {
            return builder.AddCode(CodeLineBuilder.From(code));
        }

        public static CodeBlockBuilder AddMethodCall(
            this CodeBlockBuilder builder,
            Action<MethodCallBuilder> configure)
        {
            var methodCallBuilder = new MethodCallBuilder();
            configure(methodCallBuilder);
            return builder.AddCode(methodCallBuilder);
        }

        public static CodeBlockBuilder ForEach<T>(
            this CodeBlockBuilder codeBlockBuilder,
            IEnumerable<T> enumerable,
            Action<CodeBlockBuilder, T> configure)
        {
            foreach (T element in enumerable)
            {
                configure(codeBlockBuilder, element);
            }

            return codeBlockBuilder;
        }
    }

    internal static class LambdaBuilderExtensions
    {
        public static LambdaBuilder SetCode(this LambdaBuilder builder, string code)
        {

            return builder.SetCode(CodeInlineBuilder.From(code));
        }
    }
}
