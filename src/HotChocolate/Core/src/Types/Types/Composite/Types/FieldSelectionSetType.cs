#nullable enable

using HotChocolate.Language;

namespace HotChocolate.Types.Composite;

public sealed class FieldSelectionSetType : ScalarType<SelectionSetNode, StringValueNode>
{
    public FieldSelectionSetType() : this("FieldSelectionSet")
    {
    }

    public FieldSelectionSetType(string name, BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
    }

    /// <inheritdoc />
    protected override SelectionSetNode ParseLiteral(StringValueNode valueSyntax)
    {
        try
        {
            return ParseSelectionSet(valueSyntax.Value);
        }
        catch (SyntaxException)
        {
            throw new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage("The field selection set syntax is invalid.")
                    .Build());
        }
    }

    /// <inheritdoc />
    protected override StringValueNode ParseValue(SelectionSetNode runtimeValue)
        => new(SerializeSelectionSet(runtimeValue));

    /// <inheritdoc />
    public override IValueNode ParseResult(object? resultValue)
    {
        if (resultValue is null)
        {
            return NullValueNode.Default;
        }

        if (resultValue is string s)
        {
            return new StringValueNode(s);
        }

        if (resultValue is SelectionSetNode selectionSet)
        {
            return new StringValueNode(SerializeSelectionSet(selectionSet));
        }

        throw new SerializationException(
            ErrorBuilder.New()
                .SetMessage("The field selection set syntax is invalid.")
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .Build(),
            this);
    }

    /// <inheritdoc />
    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
    {
        if (runtimeValue is null)
        {
            resultValue = null;
            return true;
        }

        if (runtimeValue is SelectionSetNode selectionSet)
        {
            resultValue = SerializeSelectionSet(selectionSet);
            return true;
        }

        resultValue = null;
        return false;
    }

    /// <inheritdoc />
    public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
    {
        if (resultValue is null)
        {
            runtimeValue = null;
            return true;
        }

        if (resultValue is SelectionSetNode selectionSet)
        {
            runtimeValue = selectionSet;
            return true;
        }

        if (resultValue is string serializedSelectionSet)
        {
            try
            {
                runtimeValue = ParseSelectionSet(serializedSelectionSet);
                return true;
            }
            catch (SyntaxException)
            {
                runtimeValue = null;
                return false;
            }
        }

        runtimeValue = null;
        return false;
    }

    internal static SelectionSetNode ParseSelectionSet(string s)
    {
        s = $"{{ {s.Trim('{', '}')} }}";
        return Utf8GraphQLParser.Syntax.ParseSelectionSet(s);
    }

    private static string SerializeSelectionSet(SelectionSetNode selectionSet)
    {
        var s = selectionSet.ToString(false);
        return s.AsSpan()[1..^1].Trim().ToString();
    }
}
