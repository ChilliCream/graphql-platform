using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetSchemaCompiler: ResolverParameterCompilerBase
    {
        private readonly PropertyInfo _schema;

        public GetSchemaCompiler()
            => _schema = PureContextType.GetProperty(nameof(IPureResolverContext.Schema))!;

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
            => ArgumentHelper.IsSchema(parameter);

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
            => Expression.Property(context, _schema);
    }
}
