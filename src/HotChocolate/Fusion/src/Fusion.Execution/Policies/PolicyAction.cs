using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Describes the guarded field occurrence an action policy evaluates: its name and its coerced
/// arguments.
/// </summary>
/// <param name="Name">
/// The occurrence's schema coordinate, formatted <c>&lt;TypeName&gt;.&lt;fieldName&gt;</c>. Always the
/// declaring type and field name, never the response alias.
/// </param>
/// <param name="Arguments">
/// The occurrence's arguments, fully coerced (variables resolved, schema defaults filled in for an
/// omitted argument or input field that declares one) and canonically ordered by argument, respectively
/// field, definition order. An omitted nullable argument with no default is absent as a key.
/// </param>
public sealed record PolicyAction(string Name, ObjectValueNode Arguments);
