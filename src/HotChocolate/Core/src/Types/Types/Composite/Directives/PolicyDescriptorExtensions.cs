namespace HotChocolate.Types.Composite;

/// <summary>
/// Provides extension methods to apply the @policy directive with the fluent API
/// to object types, interface types, and fields.
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
    /// The consequence that applies when the policy expression denies access.
    /// </param>
    /// <returns>The object type descriptor with the @policy directive applied.</returns>
    public static IObjectTypeDescriptor Policy(
        this IObjectTypeDescriptor descriptor,
        string[][] names,
        PolicyDenialBehavior onDenied = PolicyDenialBehavior.Null)
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
    /// The consequence that applies when the policy expression denies access.
    /// </param>
    /// <returns>The object type descriptor with the @policy directive applied.</returns>
    public static IObjectTypeDescriptor Policy(
        this IObjectTypeDescriptor descriptor,
        string name,
        PolicyDenialBehavior onDenied = PolicyDenialBehavior.Null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return descriptor.Directive(new Policy(name, onDenied));
    }

    /// <summary>
    /// <para>
    /// Applies the @policy directive to this interface type to restrict access with a policy
    /// expression in disjunctive normal form. Names within an inner list combine with AND,
    /// the outer list combines with OR.
    /// </para>
    /// <para>
    /// @policy(names: [["isAdmin", "isFinance"], ["isOwner"]])
    /// </para>
    /// </summary>
    /// <param name="descriptor">The interface type descriptor.</param>
    /// <param name="names">The policy expression in disjunctive normal form.</param>
    /// <param name="onDenied">
    /// The consequence that applies when the policy expression denies access.
    /// </param>
    /// <returns>The interface type descriptor with the @policy directive applied.</returns>
    public static IInterfaceTypeDescriptor Policy(
        this IInterfaceTypeDescriptor descriptor,
        string[][] names,
        PolicyDenialBehavior onDenied = PolicyDenialBehavior.Null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(names);

        return descriptor.Directive(new Policy(names, onDenied));
    }

    /// <summary>
    /// <para>
    /// Applies the @policy directive to this interface type to restrict access with a policy
    /// expression that consists of a single policy name.
    /// </para>
    /// <para>
    /// @policy(names: "hasAccess")
    /// </para>
    /// </summary>
    /// <param name="descriptor">The interface type descriptor.</param>
    /// <param name="name">The policy name.</param>
    /// <param name="onDenied">
    /// The consequence that applies when the policy expression denies access.
    /// </param>
    /// <returns>The interface type descriptor with the @policy directive applied.</returns>
    public static IInterfaceTypeDescriptor Policy(
        this IInterfaceTypeDescriptor descriptor,
        string name,
        PolicyDenialBehavior onDenied = PolicyDenialBehavior.Null)
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
    /// The consequence that applies when the policy expression denies access.
    /// </param>
    /// <returns>The object field descriptor with the @policy directive applied.</returns>
    public static IObjectFieldDescriptor Policy(
        this IObjectFieldDescriptor descriptor,
        string[][] names,
        PolicyDenialBehavior onDenied = PolicyDenialBehavior.Null)
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
    /// The consequence that applies when the policy expression denies access.
    /// </param>
    /// <returns>The object field descriptor with the @policy directive applied.</returns>
    public static IObjectFieldDescriptor Policy(
        this IObjectFieldDescriptor descriptor,
        string name,
        PolicyDenialBehavior onDenied = PolicyDenialBehavior.Null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return descriptor.Directive(new Policy(name, onDenied));
    }

    /// <summary>
    /// <para>
    /// Applies the @policy directive to this interface field to restrict access with a policy
    /// expression in disjunctive normal form. Names within an inner list combine with AND,
    /// the outer list combines with OR.
    /// </para>
    /// <para>
    /// @policy(names: [["isAdmin", "isFinance"], ["isOwner"]])
    /// </para>
    /// </summary>
    /// <param name="descriptor">The interface field descriptor.</param>
    /// <param name="names">The policy expression in disjunctive normal form.</param>
    /// <param name="onDenied">
    /// The consequence that applies when the policy expression denies access.
    /// </param>
    /// <returns>The interface field descriptor with the @policy directive applied.</returns>
    public static IInterfaceFieldDescriptor Policy(
        this IInterfaceFieldDescriptor descriptor,
        string[][] names,
        PolicyDenialBehavior onDenied = PolicyDenialBehavior.Null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(names);

        return descriptor.Directive(new Policy(names, onDenied));
    }

    /// <summary>
    /// <para>
    /// Applies the @policy directive to this interface field to restrict access with a policy
    /// expression that consists of a single policy name.
    /// </para>
    /// <para>
    /// @policy(names: "hasAccess")
    /// </para>
    /// </summary>
    /// <param name="descriptor">The interface field descriptor.</param>
    /// <param name="name">The policy name.</param>
    /// <param name="onDenied">
    /// The consequence that applies when the policy expression denies access.
    /// </param>
    /// <returns>The interface field descriptor with the @policy directive applied.</returns>
    public static IInterfaceFieldDescriptor Policy(
        this IInterfaceFieldDescriptor descriptor,
        string name,
        PolicyDenialBehavior onDenied = PolicyDenialBehavior.Null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return descriptor.Directive(new Policy(name, onDenied));
    }
}
