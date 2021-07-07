using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class SchemaParameterExpressionBuilder : IParameterExpressionBuilder
    {
        private static readonly PropertyInfo _schema;

        static SchemaParameterExpressionBuilder()
        {
            _schema = PureContextType.GetProperty(nameof(IPureResolverContext.Schema))!;
            Debug.Assert(_schema is not null!, "Schema property is missing." );
        }

        public ArgumentKind Kind => ArgumentKind.Schema;

        public bool IsPure => true;

        public bool CanHandle(ParameterInfo parameter, Type source)
            => typeof(ISchema) == parameter.ParameterType ||
               typeof(Schema) == parameter.ParameterType;

        public Expression Build(ParameterInfo parameter, Type source, Expression context)
            => Expression.Convert(Expression.Property(context, _schema), parameter.ParameterType);
    }
}
