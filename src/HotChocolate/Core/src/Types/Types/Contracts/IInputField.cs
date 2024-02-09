#nullable enable

using System.Reflection;

namespace HotChocolate.Types;

/// <summary>
/// Represents an input field. Input fields can be arguments of fields
/// or fields of an input objects.
/// </summary>
public interface IInputField : IField, IInputFieldInfo
{
    /// <summary>
    /// Defines if this field is deprecated.
    /// </summary>
    bool IsDeprecated { get; }

    /// <summary>
    /// Gets the deprecation reason.
    /// </summary>
    string? DeprecationReason { get; }
}

internal interface IHasProperty
{
    PropertyInfo? Property { get; }
}

internal interface IHasOptional
{
    bool IsOptional { get; }
}
