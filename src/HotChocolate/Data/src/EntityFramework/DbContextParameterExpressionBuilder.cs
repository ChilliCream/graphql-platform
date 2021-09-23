using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data
{
    internal sealed class DbContextParameterExpressionBuilder : IParameterExpressionBuilder
    {
        private static readonly MethodInfo _getLocalState =
            typeof(ExpressionHelper).GetMethod(
                nameof(ExpressionHelper.GetLocalState))!;

        private static readonly PropertyInfo _localContextDataProperty =
            typeof(IResolverContext).GetProperty(nameof(IResolverContext.LocalContextData))!;

        public ArgumentKind Kind => ArgumentKind.Custom;

        public bool IsPure => false;

        public bool CanHandle(ParameterInfo parameter)
            => typeof(DbContext).IsAssignableFrom(parameter.ParameterType) &&
               !parameter.GetCustomAttributesData().Any();

        public Expression Build(ParameterInfo parameter, Expression context)
        {
            Type parameterType = parameter.ParameterType;

            MemberExpression contextData = Expression.Property(context, _localContextDataProperty);

            MethodInfo getLocalState = _getLocalState.MakeGenericMethod(parameterType);

            string key = parameterType.FullName ?? parameterType.Name;
            ConstantExpression keyExpression = Expression.Constant(key, typeof(string));

            return Expression.Call(getLocalState, contextData, keyExpression);
        }
    }


}
