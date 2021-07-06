using System;
using System.Reflection;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal abstract class CustomContextCompilerBase : ResolverParameterCompilerBase
    {
        protected CustomContextCompilerBase()
        {
            ContextData = typeof(IHasContextData).GetProperty(
                nameof(IHasContextData.ContextData))!;
            ScopedContextData = ContextType.GetProperty(
                nameof(IResolverContext.ScopedContextData))!;
            LocalContextData = ContextType.GetProperty(
                nameof(IResolverContext.LocalContextData))!;
        }

        protected PropertyInfo ContextData { get; }

        protected PropertyInfo ScopedContextData { get; }

        protected PropertyInfo LocalContextData { get; }
    }
}
