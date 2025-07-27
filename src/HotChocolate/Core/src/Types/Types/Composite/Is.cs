using System.Reflection;
using HotChocolate.Fusion.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// The @is directive is utilized on lookup fields to describe how the arguments
/// can be mapped from the entity type that the lookup field resolves.
/// </para>
/// <para>
/// The mapping establishes semantic equivalence between disparate type system members
/// across source schemas and is used in cases where an argument does not directly align
/// with a field on the entity type.
/// </para>
/// <para>
/// directive @is(field: FieldSelectionMap!) on ARGUMENT_DEFINITION
/// </para>
/// <para>
/// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--is"/>
/// </para>
/// </summary>
[DirectiveType(
    DirectiveNames.Lookup.Name,
    DirectiveLocation.ArgumentDefinition,
    IsRepeatable = false)]
public sealed class Is
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Is"/> class.
    /// </summary>
    /// <param name="field">The field selection map.</param>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="field"/> is <c>null</c>.
    /// </exception>
    public Is(IValueSelectionNode field)
    {
        ArgumentNullException.ThrowIfNull(field);
        Field = field;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Is"/> class.
    /// </summary>
    /// <param name="field">The field selection map.</param>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="field"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="FieldSelectionMapSyntaxException">
    /// The syntax used in the <paramref name="field"/> parameter is invalid.
    /// </exception>
    public Is(string field)
    {
        ArgumentNullException.ThrowIfNull(field);
        Field = FieldSelectionMapParser.Parse(field);
    }

    /// <summary>
    /// Gets the field selection map.
    /// </summary>
    public IValueSelectionNode Field { get; }

    /// <inheritdoc />
    public override string ToString() => $"@is(field: \"{Field.ToString(false)}\")";
}

public sealed class IsAttribute : ArgumentDescriptorAttribute
{
    public IsAttribute(string field)
    {
        ArgumentNullException.ThrowIfNull(field);
        Field = field;
    }

    public string Field { get; }

    protected override void OnConfigure(
        IDescriptorContext context,
        IArgumentDescriptor descriptor,
        ParameterInfo parameter)
        => descriptor.Is(Field);
}

public static class IsDescriptorExtensions
{
    public static IArgumentDescriptor Is(
        this IArgumentDescriptor descriptor,
        string field)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrEmpty(field);

        IValueSelectionNode valueSelection;

        try
        {
            valueSelection = FieldSelectionMapParser.Parse(field);
        }
        catch (FieldSelectionMapSyntaxException ex)
        {
            descriptor.Extend().OnBeforeNaming(
                (ctx, _) => ctx.ReportError(
                    SchemaErrorBuilder.New()
                        .SetMessage("The field selection map syntax is invalid.")
                        .SetException(ex)
                        .Build()));
            return descriptor;
        }

        return descriptor.Directive(new Is(valueSelection));
    }
}
