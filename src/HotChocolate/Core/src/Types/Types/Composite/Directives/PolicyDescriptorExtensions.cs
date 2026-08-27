namespace HotChocolate.Types.Composite;

/// <summary>
/// Provides extension methods to apply the @policy directive with the fluent API
/// to object types and fields.
/// </summary>
public static class PolicyDescriptorExtensions
{
    /// <summary>
    /// <para>
    /// Applies the @policy directive to this object type to restrict access with a policy
    /// expression in disjunctive normal form. Names within an inner list combine with AND,
    /// the outer list combines with OR.
    /// </para>
    /// <para>
    /// @policy(names: [["isAdmin", "isFinance"], ["isOwner"]])
    /// </para>
    /// </summary>
    /// <param name="descriptor">The object type descriptor.</param>
    /// <param name="names">The policy expression in disjunctive normal form.</param>
    /// <param name="onDenied">
    /// The consequence that applies when the policy expression denies access, or
    /// <c>null</c> to inherit the schema-wide default.
    /// </param>
    /// <returns>The object type descriptor with the @policy directive applied.</returns>
    public static IObjectTypeDescriptor Policy(
        this IObjectTypeDescriptor descriptor,
        string[][] names,
        PolicyDenialBehavior? onDenied = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(names);

        return descriptor.Directive(new Policy(names, onDenied));
    }

    /// <summary>
    /// <para>
    /// Applies the @policy directive to this object type to restrict access with a policy
    /// expression that consists of a single policy name.
    /// </para>
    /// <para>
    /// @policy(names: "hasAccess")
    /// </para>
    /// </summary>
    /// <param name="descriptor">The object type descriptor.</param>
    /// <param name="name">The policy name.</param>
    /// <param name="onDenied">
    /// The consequence that applies when the policy expression denies access, or
    /// <c>null</c> to inherit the schema-wide default.
    /// </param>
    /// <returns>The object type descriptor with the @policy directive applied.</returns>
    public static IObjectTypeDescriptor Policy(
        this IObjectTypeDescriptor descriptor,
        string name,
        PolicyDenialBehavior? onDenied = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return descriptor.Directive(new Policy(name, onDenied));
    }

    /// <summary>
    /// <para>
    /// Applies the @policy directive to this object field to restrict access with a policy
    /// expression in disjunctive normal form. Names within an inner list combine with AND,
    /// the outer list combines with OR.
    /// </para>
    /// <para>
    /// @policy(names: [["isAdmin", "isFinance"], ["isOwner"]])
    /// </para>
    /// </summary>
    /// <param name="descriptor">The object field descriptor.</param>
    /// <param name="names">The policy expression in disjunctive normal form.</param>
    /// <param name="onDenied">
    /// The consequence that applies when the policy expression denies access, or
    /// <c>null</c> to inherit the schema-wide default.
    /// </param>
    /// <returns>The object field descriptor with the @policy directive applied.</returns>
    public static IObjectFieldDescriptor Policy(
        this IObjectFieldDescriptor descriptor,
        string[][] names,
        PolicyDenialBehavior? onDenied = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(names);

        return descriptor.Directive(new Policy(names, onDenied));
    }

    /// <summary>
    /// <para>
    /// Applies the @policy directive to this object field to restrict access with a policy
    /// expression that consists of a single policy name.
    /// </para>
    /// <para>
    /// @policy(names: "hasAccess")
    /// </para>
    /// </summary>
    /// <param name="descriptor">The object field descriptor.</param>
    /// <param name="name">The policy name.</param>
    /// <param name="onDenied">
    /// The consequence that applies when the policy expression denies access, or
    /// <c>null</c> to inherit the schema-wide default.
    /// </param>
    /// <returns>The object field descriptor with the @policy directive applied.</returns>
    public static IObjectFieldDescriptor Policy(
        this IObjectFieldDescriptor descriptor,
        string name,
        PolicyDenialBehavior? onDenied = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return descriptor.Directive(new Policy(name, onDenied));
    }
}
