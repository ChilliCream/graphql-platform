using System;
using System.Collections.Generic;

namespace HotChocolate.Data.ExpressionNodes;

public interface IVariableContext
{
    IReadOnlyDictionary<Identifier, BoxExpressions> Expressions { get; }
    IReadOnlyDictionary<Identifier, IBox> Boxes { get; }
}

public sealed class VariableContext : IVariableContext
{
    public IReadOnlyDictionary<Identifier, BoxExpressions> Expressions { get; }
    public IReadOnlyDictionary<Identifier, IBox> Boxes { get; }

    public VariableContext(
        IReadOnlyDictionary<Identifier, BoxExpressions> expressions,
        IReadOnlyDictionary<Identifier, IBox> boxes)
    {
        Expressions = expressions;
        Boxes = boxes;
    }
}

public static class ParameterContextExtensions
{
    public static BoxExpressions GetExpressions(
        this IVariableContext context, Identifier id)
    {
        return context.Expressions[id];
    }

    public static VariableExpressionsEnumerable GetEnumerable(
        this IVariableContext context, StructuralDependencies dependencies)
    {
        return new VariableExpressionsEnumerable(dependencies, context);
    }

    public static VariableExpressionsEnumerable.Enumerator GetEnumerator(
        this IVariableContext context, StructuralDependencies dependencies)
    {
        return GetEnumerable(context, dependencies).GetEnumerator();
    }

    public static T? GetValue<T>(
        this IVariableContext context, Identifier<T> parameterId)

        where T : IEquatable<T>
    {
        return ((Box<T>) context.Boxes[parameterId]).Value;
    }
}
