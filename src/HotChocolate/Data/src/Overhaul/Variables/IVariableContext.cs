using System.Collections.Generic;

namespace HotChocolate.Data.ExpressionNodes;

public interface IVariableContext
{
    IReadOnlyDictionary<Identifier, BoxExpression> Expressions { get; }
    IReadOnlyDictionary<Identifier, IBox> Boxes { get; }
}

public static class ParameterContextExtensions
{
    public static BoxExpression GetParameter(
        this IVariableContext context, Identifier id)
    {
        return context.Expressions[id];
    }

    public static VariableExpressionsEnumerable GetEnumerable(
        this IVariableContext context, ReadOnlyStructuralDependencies dependencies)
    {
        return new VariableExpressionsEnumerable(dependencies, context);
    }

    public static VariableExpressionsEnumerable.Enumerator GetEnumerator(
        this IVariableContext context, ReadOnlyStructuralDependencies dependencies)
    {
        return new VariableExpressionsEnumerable(dependencies, context).GetEnumerator();
    }

    public static T? GetValue<T>(
        this IVariableContext context, Identifier<T> parameterId)
    {
        return (T?) context.Boxes[parameterId];
    }
}
