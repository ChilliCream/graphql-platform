using HotChocolate.Language;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents a variable declaration that can be used to declare state for the @resolve directive.
/// </summary>
/// <param name="name">
/// The name of the variable that shall be declared.
/// </param>
/// <param name="select">
/// The field selection syntax that refers to a field
/// relative to the current type and specifies it as state.
/// </param>
/// <param name="from">
/// The subgraph the declaration refers to.
/// If set to <c>null</c> it will match state from all applicable subgraphs.
/// </param>
internal sealed class DeclareDirective(string name, FieldNode select, string? from = null)
{
    /// <summary>
    /// Gets the name of the variable that shall be declared.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the field selection syntax that refers to a field relative
    /// to the current type and specifies it as state.
    /// </summary>
    public FieldNode Select { get; } = select;

    /// <summary>
    /// Gets the subgraph the declaration refers to.
    /// If set to <c>null</c> it will match state from all applicable subgraphs.
    /// </summary>
    public string? From { get; } = from;
}