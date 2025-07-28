#nullable enable

using HotChocolate.Language;

namespace HotChocolate.Types.Composite;

/// <summary>
/// Provides extension methods to <see cref="IObjectFieldDescriptor"/> and <see cref="IInterfaceFieldDescriptor"/>
/// to apply the @key directive.
/// </summary>
public static class KeyDescriptorExtensions
{
    /// <summary>
    /// <para>
    /// Adds a @key directive to this object type to specify the fields that make up the unique key for an entity.
    /// </para>
    /// <para>
    /// One can specify multiple @key directives for an object type.
    /// </para>
    /// <para>
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--key"/>
    /// </para>
    /// </summary>
    /// <param name="descriptor">The object type descriptor.</param>
    /// <param name="fields">The fields that are used to identify an entity.</param>
    /// <returns>The object type descriptor with the @key directive applied.</returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> or the paramref name="fields"/> parameter is <c>null</c>.
    /// </exception>
    /// <exception cref="SyntaxException">
    /// The syntax used in the <paramref name="fields"/> parameter is invalid.
    /// </exception>
    public static IObjectTypeDescriptor Key(
        this IObjectTypeDescriptor descriptor,
        string fields)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrEmpty(fields);

        SelectionSetNode selectionSet;

        try
        {
            fields = $"{{ {fields.Trim('{', '}')} }}";
            selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet(fields);
        }
        catch (SyntaxException ex)
        {
            descriptor.Extend().OnBeforeNaming(
                (ctx, _) => ctx.ReportError(
                    SchemaErrorBuilder.New()
                        .SetMessage("The field selection set syntax is invalid.")
                        .SetException(ex)
                        .Build()));
            return descriptor;
        }

        return descriptor.Directive(new Key(selectionSet));
    }

    /// <summary>
    /// <para>
    /// Adds a @key directive to this interface type to specify the fields that make up the unique key for an entity.
    /// </para>
    /// <para>
    /// One can specify multiple @key directives for an interface type.
    /// </para>
    /// <para>
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--key"/>
    /// </para>
    /// </summary>
    /// <param name="descriptor">The interface type descriptor.</param>
    /// <param name="fields">The fields that are used to identify an entity.</param>
    /// <returns>The interface type descriptor with the @key directive applied.</returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> or the paramref name="fields"/> parameter is <c>null</c>.
    /// </exception>
    /// <exception cref="SyntaxException">
    /// The syntax used in the <paramref name="fields"/> parameter is invalid.
    /// </exception>
    public static IInterfaceTypeDescriptor Key(
        this IInterfaceTypeDescriptor descriptor,
        string fields)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(fields);

        SelectionSetNode selectionSet;

        try
        {
            fields = $"{{ {fields.Trim('{', '}')} }}";
            selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet(fields);
        }
        catch (SyntaxException ex)
        {
            descriptor.Extend().OnBeforeNaming(
                (ctx, _) => ctx.ReportError(
                    SchemaErrorBuilder.New()
                        .SetMessage("The field selection set syntax is invalid.")
                        .SetException(ex)
                        .Build()));
            return descriptor;
        }

        return descriptor.Directive(new Key(selectionSet));
    }
}
