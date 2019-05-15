using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal interface IResolverParameterCompiler
    {
        bool CanHandle(
            ParameterInfo parameter,
            Type sourceType);

        Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType);
    }
}
