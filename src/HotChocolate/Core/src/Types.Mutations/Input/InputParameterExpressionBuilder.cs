using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;
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
            ReflectionUtils.ExtractMethod<Dictionary<string, object>>(x => x.ContainsKey(default!));

        private static readonly PropertyInfo _getValue =
            typeof(Dictionary<string, object>).GetProperty("Item")!;

        private static readonly Expression _null = Constant(null);

        public ArgumentKind Kind => ArgumentKind.Argument;

        public bool IsPure => true;

        public bool CanHandle(ParameterInfo parameter)
        {
            InputAttribute? attribute = parameter.GetCustomAttribute<InputAttribute>() ??
                parameter.Member.GetCustomAttribute<InputAttribute>();
            return attribute is not null;
        }

        public Expression Build(ParameterInfo parameter, Expression context)
        {
            InputAttribute? attribute = parameter.GetCustomAttribute<InputAttribute>() ??
                parameter.Member.GetCustomAttribute<InputAttribute>();

            if (attribute is null)
            {
                throw new ArgumentException("Could not find the InputAttribute", nameof(parameter));
            }

            Expression argumentValue =
                Call(context,
                    _getArgumentValue,
                    Convert(Constant(attribute.ArgumentName), typeof(NameString)));

            Expression expr =
                Condition(
                    And(
                        NotEqual(argumentValue, _null),
                        Call(
                            argumentValue,
                            _containsKey,
                            Constant(parameter.Name!))),
                    // if
                    Convert(
                        Property(argumentValue, _getValue, Constant(parameter.Name)),
                        parameter.ParameterType),
                    // else
                    Convert(_null, parameter.ParameterType));

            return expr;
        }
    }
}
