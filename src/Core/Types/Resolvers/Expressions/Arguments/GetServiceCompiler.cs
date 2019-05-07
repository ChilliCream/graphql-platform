using System;
using System.Reflection;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetServiceCompiler<T>
        : GetFromGenericMethodCompilerBase<T>
        where T : IResolverContext
    {
        public GetServiceCompiler()
        {
            GenericMethod = ContextTypeInfo.GetDeclaredMethod(
                nameof(IResolverContext.Service));
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
        {
            return parameter.IsDefined(typeof(ServiceAttribute));
        }
    }
}
