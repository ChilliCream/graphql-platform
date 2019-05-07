using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal interface IResolverParameterCompiler
    {
        bool CanHandle(ParameterInfo parameter, Type sourceType);

        Expression Compile(ParameterInfo parameter, Type sourceType);
    }
}
