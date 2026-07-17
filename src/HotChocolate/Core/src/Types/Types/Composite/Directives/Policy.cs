using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// The @policy directive restricts access to the annotated type or field with a policy
/// expression in disjunctive normal form. Names within an inner list combine with AND,
/// the outer list combines with OR. The expression [["a", "b"], ["c"]] reads as
/// (a AND b) OR c. Access is granted only when the expression evaluates to true.
/// </para>
/// <para>
/// The onDenied argument defines the consequence when the expression does not evaluate
/// to true: NULL sets the guarded value to null without an error, ERROR sets it to null
/// and adds an authorization error, ABORT terminates the request.
/// </para>
/// <para>
/// Repeated applications on the same member combine with AND and the most severe
/// consequence wins.
/// </para>
/// <para>
/// directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior! = NULL) repeatable on OBJECT | INTERFACE | FIELD_DEFINITION
/// </para>
/// </summary>
[DirectiveType(
    DirectiveNames.Policy.Name,
    DirectiveLocation.Object | DirectiveLocation.Interface | DirectiveLocation.FieldDefinition,
    IsRepeatable = true)]
[PolicySyntax]
public sealed class Policy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Policy"/> class.
    /// </summary>
    /// <param name="names">
    /// The policy expression in disjunctive normal form. Names within an inner list
    /// combine with AND, the outer list combines with OR.
    /// </param>
    /// <param name="onDenied">
    /// The consequence that applies when the policy expression denies access.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="names"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The <paramref name="names"/> contains no group, a group contains no name,
    /// or a name is empty, whitespace, or has leading or trailing whitespace.
    /// </exception>
    public Policy(
        IReadOnlyList<IReadOnlyList<string>> names,
        PolicyDenialBehavior onDenied = PolicyDenialBehavior.Null)
    {
        ArgumentNullException.ThrowIfNull(names);

        if (names.Count == 0)
        {
            throw new ArgumentException(
                "The policy expression must contain at least one policy name group.",
                nameof(names));
        }

        foreach (var group in names)
        {
            if (group is null || group.Count == 0)
            {
                throw new ArgumentException(
                    "A policy name group must contain at least one policy name.",
                    nameof(names));
            }

            foreach (var name in group)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException(
                        "A policy name must not be empty or whitespace.",
                        nameof(names));
                }

                if (char.IsWhiteSpace(name[0]) || char.IsWhiteSpace(name[^1]))
                {
                    throw new ArgumentException(
                        "A policy name must not have leading or trailing whitespace.",
                        nameof(names));
                }
            }
        }

        Names = names;
        OnDenied = onDenied;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Policy"/> class with a policy
    /// expression that consists of a single policy name.
    /// </summary>
    /// <param name="name">The policy name.</param>
    /// <param name="onDenied">
    /// The consequence that applies when the policy expression denies access.
    /// </param>
    /// <exception cref="ArgumentException">
    /// The <paramref name="name"/> is <c>null</c>, empty, whitespace, or has leading
    /// or trailing whitespace.
    /// </exception>
    public Policy(string name, PolicyDenialBehavior onDenied = PolicyDenialBehavior.Null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (char.IsWhiteSpace(name[0]) || char.IsWhiteSpace(name[^1]))
        {
            throw new ArgumentException(
                "A policy name must not have leading or trailing whitespace.",
                nameof(name));
        }

        Names = new[] { new[] { name } };
        OnDenied = onDenied;
    }

    /// <summary>
    /// Gets the policy expression in disjunctive normal form. Names within an inner list
    /// combine with AND, the outer list combines with OR.
    /// </summary>
    [GraphQLName(DirectiveNames.Policy.Arguments.Names)]
    [GraphQLDescription(
        "The policy expression in disjunctive normal form. Names within an inner list "
        + "combine with AND, the outer list combines with OR.")]
    [GraphQLType<NonNullType<ListType<NonNullType<ListType<NonNullType<StringType>>>>>>]
    public IReadOnlyList<IReadOnlyList<string>> Names { get; }

    /// <summary>
    /// Gets the consequence that applies when the policy expression denies access.
    /// </summary>
    [GraphQLName(DirectiveNames.Policy.Arguments.OnDenied)]
    [GraphQLDescription("The consequence that applies when the policy expression denies access.")]
    [GraphQLType<NonNullType<PolicyDenialBehaviorType>>]
    [DefaultValueSyntax("NULL")]
    public PolicyDenialBehavior OnDenied { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        var names = FormatNames(Names).ToString(false);

        return OnDenied is PolicyDenialBehavior.Null
            ? $"@policy(names: {names})"
            : $"@policy(names: {names}, onDenied: {FormatOnDenied(OnDenied).Value})";
    }

    internal static IValueNode FormatNames(IReadOnlyList<IReadOnlyList<string>> names)
    {
        if (names.Count == 1 && names[0].Count == 1)
        {
            return new StringValueNode(names[0][0]);
        }

        var groups = new IValueNode[names.Count];

        for (var i = 0; i < names.Count; i++)
        {
            var group = names[i];
            var items = new IValueNode[group.Count];

            for (var j = 0; j < group.Count; j++)
            {
                items[j] = new StringValueNode(group[j]);
            }

            groups[i] = new ListValueNode(items);
        }

        return new ListValueNode(groups);
    }

    internal static EnumValueNode FormatOnDenied(PolicyDenialBehavior onDenied)
        => onDenied switch
        {
            PolicyDenialBehavior.Null => new EnumValueNode("NULL"),
            PolicyDenialBehavior.Error => new EnumValueNode("ERROR"),
            PolicyDenialBehavior.Abort => new EnumValueNode("ABORT"),
            _ => throw new InvalidOperationException(
                $"The value `{onDenied}` is not supported by the @policy onDenied argument.")
        };
}

file sealed class PolicySyntaxAttribute : DirectiveTypeDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IDirectiveTypeDescriptor descriptor,
        Type? type)
    {
        descriptor.ExtendWith(static extension =>
        {
            extension.Configuration.Format = static directive =>
            {
                var policy = (Policy)directive;
                var arguments = new List<ArgumentNode>
                {
                    new(DirectiveNames.Policy.Arguments.Names, Policy.FormatNames(policy.Names))
                };

                if (policy.OnDenied is not PolicyDenialBehavior.Null)
                {
                    arguments.Add(
                        new ArgumentNode(
                            DirectiveNames.Policy.Arguments.OnDenied,
                            Policy.FormatOnDenied(policy.OnDenied)));
                }

                return new DirectiveNode(DirectiveNames.Policy.Name, arguments);
            };

            extension.Configuration.Parse = static directiveNode =>
            {
                var namesArg = directiveNode.Arguments.FirstOrDefault(
                    t => t.Name.Value.Equals(DirectiveNames.Policy.Arguments.Names));
                var onDeniedArg = directiveNode.Arguments.FirstOrDefault(
                    t => t.Name.Value.Equals(DirectiveNames.Policy.Arguments.OnDenied));

                if (namesArg is null)
                {
                    throw new InvalidOperationException(
                        "Cannot parse the @policy directive as it is missing the names argument.");
                }

                return new Policy(ParseNames(namesArg.Value), ParseOnDenied(onDeniedArg?.Value));
            };
        });
    }

    private static IReadOnlyList<IReadOnlyList<string>> ParseNames(IValueNode value)
    {
        switch (value)
        {
            case StringValueNode stringValue:
                return new[] { new[] { stringValue.Value } };

            case ListValueNode listValue:
                var groups = new IReadOnlyList<string>[listValue.Items.Count];

                for (var i = 0; i < listValue.Items.Count; i++)
                {
                    groups[i] = ParseGroup(listValue.Items[i]);
                }

                return groups;

            default:
                throw new InvalidOperationException(
                    "The names argument on @policy must be a string or a list of "
                    + "policy name groups.");
        }
    }

    private static IReadOnlyList<string> ParseGroup(IValueNode value)
    {
        switch (value)
        {
            case StringValueNode stringValue:
                return new[] { stringValue.Value };

            case ListValueNode listValue:
                var names = new string[listValue.Items.Count];

                for (var i = 0; i < listValue.Items.Count; i++)
                {
                    if (listValue.Items[i] is not StringValueNode nameValue)
                    {
                        throw new InvalidOperationException(
                            "A policy name on @policy must be a string.");
                    }

                    names[i] = nameValue.Value;
                }

                return names;

            default:
                throw new InvalidOperationException(
                    "A policy name group on @policy must be a string or a list of strings.");
        }
    }

    private static PolicyDenialBehavior ParseOnDenied(IValueNode? value)
    {
        if (value is null)
        {
            return PolicyDenialBehavior.Null;
        }

        if (value is EnumValueNode enumValue)
        {
            switch (enumValue.Value)
            {
                case "NULL":
                    return PolicyDenialBehavior.Null;

                case "ERROR":
                    return PolicyDenialBehavior.Error;

                case "ABORT":
                    return PolicyDenialBehavior.Abort;
            }
        }

        throw new InvalidOperationException(
            "The onDenied argument on @policy must be one of the enum values "
            + "NULL, ERROR, or ABORT.");
    }
}
