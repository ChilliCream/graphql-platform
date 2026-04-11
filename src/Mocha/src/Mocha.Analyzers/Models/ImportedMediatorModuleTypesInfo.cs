using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers;

/// <summary>
/// Represents the set of types imported from a referenced module's
/// <c>[MediatorModuleInfo]</c> attribute. Each instance corresponds to a single
/// invocation of a module registration method (e.g. <c>builder.AddOrderService()</c>).
/// </summary>
/// <param name="MethodName">The name of the invoked module registration method.</param>
/// <param name="ImportedTypeNames">
/// The fully qualified type names listed in the <c>MessageTypes</c> property of the attribute.
/// </param>
/// <param name="ImportedHandlerTypeNames">
/// The fully qualified type names listed in the <c>HandlerTypes</c> property of the attribute.
/// </param>
public sealed record ImportedMediatorModuleTypesInfo(
    string MethodName,
    ImmutableEquatableArray<string> ImportedTypeNames,
    ImmutableEquatableArray<string> ImportedHandlerTypeNames) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => $"ImportedMediatorModule:{MethodName}";
}
