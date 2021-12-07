namespace HotChocolate.Types;

internal class InputArgumentParameterExpressionBuilder : InputParameterExpressionBuilder
{
    public override bool IsDefaultHandler => true;

    public override bool CanHandle(ParameterInfo parameter)
        => parameter.Member.IsDefined(typeof(InputAttribute));
}
