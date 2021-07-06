using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetServiceCompiler : GetFromGenericMethodCompilerBase
    {
        private const string _service = nameof(IPureResolverContext.Service);

        public GetServiceCompiler()
        {
            GenericMethod = PureContextType.GetMethods().First(IsServiceMethod);

            bool IsServiceMethod(MethodInfo method)
                => method.Name.Equals(_service, StringComparison.Ordinal) &&
                    method.IsGenericMethod;
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
            => ArgumentHelper.IsService(parameter);
    }
}
