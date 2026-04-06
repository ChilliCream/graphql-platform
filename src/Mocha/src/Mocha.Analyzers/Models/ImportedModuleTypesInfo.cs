using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers;

/// <summary>
/// Represents the set of message types imported from a referenced module's
/// <c>[MessagingModuleInfo]</c> attribute. Each instance corresponds to a single
/// invocation of a module registration method (e.g. <c>builder.AddOrderService()</c>).
/// </summary>
/// <param name="MethodName">The name of the invoked module registration method.</param>
/// <param name="ImportedTypeNames">
/// The fully qualified type names listed in the <c>MessageTypes</c> property of the attribute.
/// </param>
public sealed record ImportedModuleTypesInfo(
    string MethodName,
    ImmutableEquatableArray<string> ImportedTypeNames) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => $"ImportedModule:{MethodName}";
}
