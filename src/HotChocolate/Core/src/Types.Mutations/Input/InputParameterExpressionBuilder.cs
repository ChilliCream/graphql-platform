using System.Linq.Expressions;
using HotChocolate.Types.Properties;
using static System.Linq.Expressions.Expression;

#nullable enable

namespace HotChocolate.Types
{
    internal class InputParameterExpressionBuilder : IParameterExpressionBuilder
    {
        private static readonly MethodInfo _getArgumentValue =
            ReflectionUtils.ExtractMethod<IPureResolverContext>(
                x => x.ArgumentValue<Dictionary<string, object>>(default));

        private static readonly MethodInfo _containsKey =
            ReflectionUtils.ExtractMethod<Dictionary<string, object>>(
                x => x.ContainsKey(default!));

        private static readonly PropertyInfo _getValue =
            typeof(Dictionary<string, object>).GetProperty("Item")!;

        private static readonly Expression _null = Constant(null);

        public ArgumentKind Kind => ArgumentKind.Argument;

        public bool IsPure => true;

        public virtual bool IsDefaultHandler => false;

        public virtual bool CanHandle(ParameterInfo parameter)
            => parameter.IsDefined(typeof(InputAttribute));

        public Expression Build(ParameterInfo parameter, Expression context)
        {
            InputAttribute? attribute = parameter.GetCustomAttribute<InputAttribute>() ??
                parameter.Member.GetCustomAttribute<InputAttribute>();

            if (attribute is null)
            {
                throw new ArgumentException(
                    MutationResources.InputParameterExpressionBuilder_Build_NoAttribute,
                    nameof(parameter));
            }

            ParameterExpression variable =
                Variable(typeof(Dictionary<string, object>), "val");

            return Block(new[]
                {
                    variable
                },
                Assign(
                    variable,
                    Call(context,
                        _getArgumentValue,
                        Convert(Constant(attribute.ArgumentName), typeof(NameString)))),
                Condition(
                    And(
                        NotEqual(variable, _null),
                        Call(
                            variable,
                            _containsKey,
                            Constant(parameter.Name!))),
                    // if
                    Convert(
                        Property(variable, _getValue, Constant(parameter.Name)),
                        parameter.ParameterType),
                    // else
                    Convert(_null, parameter.ParameterType))
            );
        }
    }
}
