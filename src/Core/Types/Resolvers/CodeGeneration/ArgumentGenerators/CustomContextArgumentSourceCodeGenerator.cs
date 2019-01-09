using System.Reflection;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class CustomContextArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.CustomContext;

        protected override string Generate(
            ArgumentDescriptor descriptor)
        {
            string typeName = descriptor.Type.GetTypeName();
            string key = null;

            if (descriptor.Parameter != null
                && descriptor.Parameter.IsDefined(typeof(StateAttribute)))
            {
                key = WriteEscapeCharacters(descriptor.Parameter
                    .GetCustomAttribute<StateAttribute>().Key);
            }
            else
            {
                key = WriteEscapeCharacters(typeName);
            }

            return $"ctx.{nameof(IResolverContext.CustomProperty)}<{descriptor.Type.GetTypeName()}>(\"{key}\")";
        }
    }
}
