using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetOutputFieldCompiler : ResolverParameterCompilerBase
    {
        private readonly PropertyInfo _selection;
        private readonly PropertyInfo _field;

        public GetOutputFieldCompiler()
        {
            _selection = PureContextType.GetProperty(nameof(IPureResolverContext.Selection))!;
            _field = typeof(IFieldSelection).GetProperty(nameof(IFieldSelection.Field))!;
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
            => ArgumentHelper.IsOutputField(parameter);

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
        {
            MemberExpression selection = Expression.Property(context, _selection);
            MemberExpression field = Expression.Property(selection, _field);
            return Expression.Convert(field, parameter.ParameterType);
        }
    }
}
