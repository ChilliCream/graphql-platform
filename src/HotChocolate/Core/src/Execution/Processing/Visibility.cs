using System;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing;

public readonly struct Visibility : IEquatable<Visibility>
{
    public Visibility(IValueNode skip, IValueNode include)
    {
        Skip = skip;
        Include = include;
    }

    public IValueNode Skip { get; }

    public IValueNode Include { get; }

    public bool IsVisible(IVariableValueCollection variables)
    {
        if (Skip is null || Include is null)
        {
            return true;
        }

        var skip = false;

        if (Skip.Kind is SyntaxKind.BooleanValue)
        {
            skip = ((BooleanValueNode)Skip).Value;
        }
        else if (Skip.Kind is SyntaxKind.Variable)
        {
            skip = variables.GetVariable<bool>(((VariableNode)Skip).Name.Value);
        }

        var include = true;

        if (Include.Kind is SyntaxKind.BooleanValue)
        {
            include = ((BooleanValueNode)Include).Value;
        }
        else if (Include.Kind is SyntaxKind.Variable)
        {
            include = variables.GetVariable<bool>(((VariableNode)Include).Name.Value);
        }

        return !skip && include;
    }

    public bool Equals(Visibility other)
        => Skip.Equals(other.Skip, SyntaxComparison.Syntax) &&
            Include.Equals(other.Include, SyntaxComparison.Syntax);

    public override bool Equals(object? obj)
        => obj is Visibility other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(
            SyntaxComparer.BySyntax.GetHashCode(Skip),
            SyntaxComparer.BySyntax.GetHashCode(Include));

    public static bool operator ==(Visibility left, Visibility right)
        => left.Equals(right);

    public static bool operator !=(Visibility left, Visibility right)
        => !left.Equals(right);

    public static bool TryExtract(FieldNode fieldNode, out Visibility visibility)
    {
        IValueNode? skip = null;
        IValueNode? include = null;

        if (fieldNode.Directives.Count == 0)
        {
            visibility = default;
            return false;
        }

        for (var i = 0; i < fieldNode.Directives.Count; i++)
        {
            DirectiveNode directive = fieldNode.Directives[i];

            if (directive.Arguments.Count != 1)
            {
                // the skip and include arguments have a single argument.
                continue;
            }

            if (directive.Name.Value.EqualsOrdinal(WellKnownDirectives.Skip))
            {
                skip = directive.Arguments[0].Value;
            }

            if (directive.Name.Value.EqualsOrdinal(WellKnownDirectives.Include))
            {
                include = directive.Arguments[0].Value;
            }

            if (skip is not null && include is not null)
            {
                break;
            }
        }

        if (skip is null && include is null)
        {
            visibility = default;
            return false;
        }

        visibility = new Visibility(
            skip ?? NullValueNode.Default,
            include ?? NullValueNode.Default);
        return true;
    }
}

internal readonly struct VisibilityRef
{
    public VisibilityRef(byte id, Visibility visibility)
    {
        Id = id;
        Visibility = visibility;
    }

    public byte Id { get; }

    public Visibility Visibility { get;  }
}
