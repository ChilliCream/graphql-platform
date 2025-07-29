#nullable enable

using HotChocolate.Fusion.Language;
using HotChocolate.Language;

namespace HotChocolate.Types.Composite;

public sealed class FieldSelectionMapType : ScalarType<IValueSelectionNode, StringValueNode>
{
    public FieldSelectionMapType() : this("FieldSelectionMap")
    {
    }

    public FieldSelectionMapType(string name, BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
    }

    /// <inheritdoc />
    protected override IValueSelectionNode ParseLiteral(StringValueNode valueSyntax)
    {
        try
        {
            return ParseValueSelection(valueSyntax.Value);
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
    protected override StringValueNode ParseValue(IValueSelectionNode runtimeValue)
        => new(FormatValueSelection(runtimeValue));

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

        if (resultValue is IValueSelectionNode valueSelection)
        {
            return new StringValueNode(FormatValueSelection(valueSelection));
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

        if (runtimeValue is IValueSelectionNode valueSelection)
        {
            resultValue = FormatValueSelection(valueSelection);
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

        if (resultValue is IValueSelectionNode valueSelection)
        {
            runtimeValue = valueSelection;
            return true;
        }

        if (resultValue is string serializedValueSelection)
        {
            try
            {
                runtimeValue = ParseValueSelection(serializedValueSelection);
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

    internal static IValueSelectionNode ParseValueSelection(string sourceText)
        => FieldSelectionMapParser.Parse(sourceText);

    private static string FormatValueSelection(IValueSelectionNode valueSelection)
        => valueSelection.ToString();
}
