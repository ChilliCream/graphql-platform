using HotChocolate.Language;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing;

public sealed class SelectionIncludeCondition
{
    public SelectionIncludeCondition(
        IValueNode? skip = null,
        IValueNode? include = null,
        SelectionIncludeCondition? parent = null)
    {
        if (skip is null && include is null)
        {
            throw new ArgumentException("Either skip or include have to be set.");
        }

        if (skip != null &&
            skip.Kind != SyntaxKind.Variable &&
            skip.Kind != SyntaxKind.BooleanValue)
        {
            throw new ArgumentException("skip must be a variable or a boolean value");
        }

        if (include != null &&
            include.Kind != SyntaxKind.Variable &&
            include.Kind != SyntaxKind.BooleanValue)
        {
            throw new ArgumentException("skip must be a variable or a boolean value");
        }

        Skip = skip;
        Include = include;
        Parent = parent;
    }

    public IValueNode? Skip { get; }

    public IValueNode? Include { get; }

    public SelectionIncludeCondition? Parent { get; }

    public bool IsTrue(IVariableValueCollection variables)
    {
        if (Parent != null && !Parent.IsTrue(variables))
        {
            return false;
        }

        if (Skip != null && IsTrue(variables, Skip))
        {
            return false;
        }

        return Include is null || IsTrue(variables, Include);
    }

    private static bool IsTrue(
        IVariableValueCollection variables,
        IValueNode value)
    {
        if (value is BooleanValueNode b)
        {
            return b.Value;
        }

        if (value is VariableNode v)
        {
            return variables.GetVariable<bool>(v.Name.Value);
        }

        throw FieldVisibility_ValueNotSupported(value);
    }

    public bool Equals(SelectionIncludeCondition visibility)
    {
        if (Equals(visibility.Skip, visibility.Include))
        {
            if (Parent is null)
            {
                return visibility.Parent is null;
            }

            return visibility.Parent is not null && Parent.Equals(visibility.Parent);
        }

        return false;
    }

    public bool Equals(IValueNode? skip, IValueNode? include)
        => SyntaxComparer.BySyntax.Equals(skip, Skip) &&
            SyntaxComparer.BySyntax.Equals(include, Include);
}
