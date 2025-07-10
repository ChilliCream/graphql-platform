using HotChocolate.Properties;

namespace HotChocolate.Types;

public static class RequiresOptInDirectiveExtensions
{
    /// <summary>
    /// Adds an <c>@requiresOptIn</c> directive to an <see cref="ObjectField"/>.
    /// <code>
    /// type Book {
    ///   id: ID!
    ///   title: String!
    ///   author: String @requiresOptIn(feature: "your-feature")
    /// }
    /// </code>
    /// </summary>
    /// <param name="descriptor">
    /// The <paramref name="descriptor"/> on which this directive shall be annotated.
    /// </param>
    /// <param name="feature">
    /// The name of the feature that requires opt in.
    /// </param>
    /// <returns>
    /// Returns the <paramref name="descriptor"/> on which this directive
    /// was applied for configuration chaining.
    /// </returns>
    public static IObjectFieldDescriptor RequiresOptIn(
        this IObjectFieldDescriptor descriptor,
        string feature)
    {
        ApplyRequiresOptIn(descriptor, feature);
        return descriptor;
    }

    /// <summary>
    /// Adds an <c>@requiresOptIn</c> directive to an <see cref="InputField"/>.
    /// <code>
    /// input BookInput {
    ///   title: String!
    ///   author: String!
    ///   publishedDate: String @requiresOptIn(feature: "your-feature")
    /// }
    /// </code>
    /// </summary>
    /// <param name="descriptor">
    /// The <paramref name="descriptor"/> on which this directive shall be annotated.
    /// </param>
    /// <param name="feature">
    /// The name of the feature that requires opt in.
    /// </param>
    /// <returns>
    /// Returns the <paramref name="descriptor"/> on which this directive
    /// was applied for configuration chaining.
    /// </returns>
    public static IInputFieldDescriptor RequiresOptIn(
        this IInputFieldDescriptor descriptor,
        string feature)
    {
        ApplyRequiresOptIn(descriptor, feature);
        return descriptor;
    }

    /// <summary>
    /// Adds an <c>@requiresOptIn</c> directive to an <see cref="Argument"/>.
    /// <code>
    /// type Query {
    ///   books(search: String @requiresOptIn(feature: "your-feature")): [Book]
    /// }
    /// </code>
    /// </summary>
    /// <param name="descriptor">
    /// The <paramref name="descriptor"/> on which this directive shall be annotated.
    /// </param>
    /// <param name="feature">
    /// The name of the feature that requires opt in.
    /// </param>
    /// <returns>
    /// Returns the <paramref name="descriptor"/> on which this directive
    /// was applied for configuration chaining.
    /// </returns>
    public static IArgumentDescriptor RequiresOptIn(
        this IArgumentDescriptor descriptor,
        string feature)
    {
        ApplyRequiresOptIn(descriptor, feature);
        return descriptor;
    }

    /// <summary>
    /// Adds an <c>@requiresOptIn</c> directive to an <see cref="EnumValue"/>.
    /// <code>
    /// enum Episode {
    ///   NEWHOPE @requiresOptIn(feature: "your-feature")
    ///   EMPIRE
    ///   JEDI
    /// }
    /// </code>
    /// </summary>
    /// <param name="descriptor">
    /// The <paramref name="descriptor"/> on which this directive shall be annotated.
    /// </param>
    /// <param name="feature">
    /// The name of the feature that requires opt in.
    /// </param>
    /// <returns>
    /// Returns the <paramref name="descriptor"/> on which this directive
    /// was applied for configuration chaining.
    /// </returns>
    public static IEnumValueDescriptor RequiresOptIn(
        this IEnumValueDescriptor descriptor,
        string feature)
    {
        ApplyRequiresOptIn(descriptor, feature);
        return descriptor;
    }

    private static void ApplyRequiresOptIn(this IDescriptor descriptor, string feature)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        switch (descriptor)
        {
            case IObjectFieldDescriptor desc:
                desc.Directive(new RequiresOptInDirective(feature));
                break;

            case IInputFieldDescriptor desc:
                desc.Directive(new RequiresOptInDirective(feature));
                break;

            case IArgumentDescriptor desc:
                desc.Directive(new RequiresOptInDirective(feature));
                break;

            case IEnumValueDescriptor desc:
                desc.Directive(new RequiresOptInDirective(feature));
                break;

            default:
                throw new NotSupportedException(
                    TypeResources.RequiresOptInDirective_Descriptor_NotSupported);
        }
    }
}
