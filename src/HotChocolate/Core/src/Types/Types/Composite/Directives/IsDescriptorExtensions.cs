using HotChocolate.Fusion.Language;

namespace HotChocolate.Types.Composite;

/// <summary>
/// Provides extension methods to <see cref="IArgumentDescriptor"/> to apply the @is directive.
/// </summary>
public static class IsDescriptorExtensions
{
    /// <summary>
    /// <para>
    /// Applies the @is directive to the argument to describe how the arguments
    /// can be mapped from the entity type that the lookup field resolves.
    /// </para>
    /// <para>
    /// The mapping establishes semantic equivalence between disparate type system members across
    /// source schemas and is used in cases where an argument does not directly align with a field
    /// on the entity type.
    /// </para>
    /// <para>
    /// <code language="graphql">
    /// type Query {
    ///   productById(productId: ID! @is(field: "id")): Product @lookup
    ///   user(by: UserByInput! @is(field: "{ id } | { email }")): User @lookup
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--is"/>
    /// </para>
    /// </summary>
    /// <param name="descriptor">The argument descriptor.</param>
    /// <param name="field">The field selection map.</param>
    /// <returns>The argument descriptor with the @is directive applied.</returns>
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
