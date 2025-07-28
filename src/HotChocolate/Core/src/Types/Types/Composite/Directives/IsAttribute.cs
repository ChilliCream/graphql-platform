#nullable enable

using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// Applies the @is directive to this lookup argument to describe how the argument
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
public sealed class IsAttribute : ArgumentDescriptorAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IsAttribute"/> class.
    /// </summary>
    /// <param name="field">The field selection map.</param>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="field"/> is <c>null</c>.
    /// </exception>
    public IsAttribute(string field)
    {
        ArgumentNullException.ThrowIfNull(field);
        Field = field;
    }

    /// <summary>
    /// Gets the field selection map.
    /// </summary>
    public string Field { get; }

    /// <inheritdoc />
    protected override void OnConfigure(
        IDescriptorContext context,
        IArgumentDescriptor descriptor,
        ParameterInfo parameter)
        => descriptor.Is(Field);
}
