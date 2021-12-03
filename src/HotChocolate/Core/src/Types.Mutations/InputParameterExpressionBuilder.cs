namespace HotChocolate.Types;

using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;

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

    private readonly Dictionary<ParameterInfo, NameString> _parameters;

    public InputParameterExpressionBuilder(
        Dictionary<ParameterInfo, NameString> parameterToArgument)
    {
        _parameters = parameterToArgument;
    }

    public ArgumentKind Kind => ArgumentKind.Argument;

    public bool IsPure => true;

    public virtual bool IsDefaultHandler => false;

    public virtual bool CanHandle(ParameterInfo parameter)
        => _parameters.ContainsKey(parameter);

    public Expression Build(ParameterInfo parameter, Expression context)
    {
        ParameterExpression variable = Variable(typeof(Dictionary<string, object>), "val");

        return Block(new[] {variable},
            Assign(
                variable,
                Call(context,
                    _getArgumentValue,
                    Convert(Constant(_parameters[parameter]), typeof(NameString)))),
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
