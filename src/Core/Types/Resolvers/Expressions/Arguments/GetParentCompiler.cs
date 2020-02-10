using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Properties;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetParentCompiler<T>
        : ResolverParameterCompilerBase<T>
        where T : IResolverContext
    {
        private readonly MethodInfo _genericMethod;

        public GetParentCompiler()
        {
            _genericMethod = ContextTypeInfo.GetDeclaredMethod(
                nameof(IResolverContext.Parent));
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) =>
                ArgumentHelper.IsParent(parameter, sourceType);

        public override Expression Compile(
         Expression context,
         ParameterInfo parameter,
         Type sourceType)
        {
            ParentAttribute attribute =
                parameter.GetCustomAttributes<ParentAttribute>().FirstOrDefault();

            if (attribute?.Property == null)
            {
                MethodInfo argumentMethod =
                    _genericMethod.MakeGenericMethod(parameter.ParameterType);

                return Expression.Call(context, argumentMethod);
            }
            else
            {
                if (sourceType == typeof(object))
                {
                    throw new InvalidOperationException(
                        TypeResources.ResolverCompiler_ParentPropertyInvalidSource);
                }
                MethodInfo argumentMethod = _genericMethod.MakeGenericMethod(
                    sourceType);

                Expression acessor =
                    Expression.Call(context, argumentMethod);

                PropertyInfo property = sourceType.GetProperty(attribute.Property);

                if (property == null)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            TypeResources.ResolverCompiler_ParentPropertyDoesNotExists,
                            attribute.Property,
                            sourceType.Name));
                }

                if (!parameter.ParameterType.IsAssignableFrom(property.PropertyType))
                {
                    throw new InvalidOperationException(
                        string.Format(
                            TypeResources.ResolverCompiler_ParentPropertyHasWrongType,
                            attribute.Property,
                            parameter.ParameterType.Name,
                            property.PropertyType.Name));
                }

                return Expression.Property(acessor, property);
            }
        }
    }
}
