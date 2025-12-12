using HotChocolate.Fusion.Language;

namespace HotChocolate.Types.Composite;

/// <summary>
/// Provides extension methods to <see cref="IArgumentDescriptor"/> to apply the @require directive.
/// </summary>
public static class RequireDescriptorExtensions
{
    /// <summary>
    /// <para>
    /// Applies the @require directive to this argument to express a data requirement.
    /// The data requirement can only require data from other source schemas and cannot be used
    /// to require data from the same source schema.
    /// </para>
    /// <para>
    /// @require(field: "user.name")
    /// </para>
    /// <para>
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--require"/>
    /// </para>
    /// </summary>
    /// <param name="descriptor">The argument descriptor.</param>
    /// <param name="field">The field selection map.</param>
    /// <returns>The argument descriptor with the @require directive applied.</returns>
    /// <exception cref="FieldSelectionMapSyntaxException">
    /// The syntax used in the <paramref name="field"/> parameter is invalid.
    /// </exception>
    public static IArgumentDescriptor Require(this IArgumentDescriptor descriptor, string field)
    {
        IValueSelectionNode valueSelection;

        try
        {
            valueSelection = FieldSelectionMapParser.Parse(field);
        }
        catch (FieldSelectionMapSyntaxException ex)
        {
            descriptor.Extend().OnBeforeNaming((ctx, _) => ctx.ReportError(
                SchemaErrorBuilder.New()
                    .SetMessage("The field selection map syntax is invalid.")
                    .SetException(ex)
                    .Build()));
            return descriptor;
        }

        return descriptor.Directive(new Require(valueSelection));
    }
}
