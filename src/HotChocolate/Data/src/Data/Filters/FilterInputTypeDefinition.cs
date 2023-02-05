using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters;

public class FilterInputTypeDefinition
    : InputObjectTypeDefinition
    , IHasScope
    , IFilterInputTypeDefinition
{
    private List<FilterFieldBinding>? _fieldIgnores;

    public Type? EntityType { get; set; }

    public string? Scope { get; set; }

    public bool UseOr { get; set; }

    public bool UseAnd { get; set; }

    internal bool IsNamed { get; set; }

    /// <summary>
    /// Gets fields that shall be ignored.
    /// </summary>
    public IList<FilterFieldBinding> FieldIgnores =>
        _fieldIgnores ??= new List<FilterFieldBinding>();

    /// <summary>
    /// Specifies if this definition has ignored fields.
    /// </summary>
    public bool HasFieldIgnores => _fieldIgnores is { Count: > 0 };
}

/// <summary>
/// Describes a binding to an filter field.
/// </summary>
public readonly struct FilterFieldBinding
{
    /// <summary>
    /// Creates a new instance of <see cref="FilterFieldBinding"/>.
    /// </summary>
    /// <param name="name">
    /// The binding name.
    /// </param>
    /// <param name="type">
    /// The binding type.
    /// </param>
    public FilterFieldBinding(string name, FilterFieldBindingType type)
    {
        Name = name.EnsureGraphQLName();
        Type = type;
    }

    /// <summary>
    /// Gets the binding name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the binding type.
    /// </summary>
    public FilterFieldBindingType Type { get; }
}

/// <summary>
/// Describes what a field filter binds to.
/// </summary>
public enum FilterFieldBindingType
{
    /// <summary>
    /// Binds to a property
    /// </summary>
    Property,

    /// <summary>
    /// Binds to a GraphQL field
    /// </summary>
    Field
}
