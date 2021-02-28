using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    internal static class MethodCallBuilderExtensions
    {
        public static MethodCallBuilder AddArgumentRange(
            this MethodCallBuilder builder,
            IEnumerable<string> arguments)
        {
            foreach (var argument in arguments)
            {
                builder.AddArgument(argument);
            }

            return builder;
        }
    }
}
