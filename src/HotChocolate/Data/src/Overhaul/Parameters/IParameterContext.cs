using System.Collections.Generic;

namespace HotChocolate.Data.ExpressionNodes;

public interface IParameterContext
{
    IReadOnlyDictionary<Identifier, BoxExpression> Expressions { get; }
    IReadOnlyDictionary<Identifier, IBox> Variables { get; }
}

public static class ParameterContextExtensions
{
    public static BoxExpression GetParameter(
        this IParameterContext context, Identifier id)
    {
        return context.Expressions[id];
    }

    public static ParameterBoxesEnumerable GetEnumerable(
        this IParameterContext context, ReadOnlyStructuralDependencies dependencies)
    {
        return new ParameterBoxesEnumerable(dependencies, context);
    }

    public static ParameterBoxesEnumerable.Enumerator GetEnumerator(
        this IParameterContext context, ReadOnlyStructuralDependencies dependencies)
    {
        return new ParameterBoxesEnumerable(dependencies, context).GetEnumerator();
    }

    public static T? GetValue<T>(
        this IParameterContext context, Identifier<T> parameterId)
    {
        return (T?) context.Variables[parameterId];
    }
}
