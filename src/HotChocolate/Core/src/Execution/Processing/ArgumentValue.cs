using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

public sealed class ArgumentValue : IInputFieldInfo
{
    private readonly IInputFieldInfo _argument;

    public ArgumentValue(
        IInputFieldInfo argument,
        ValueKind kind,
        bool isFinal,
        bool isImplicit,
        object? value,
        IValueNode valueLiteral)
    {
        _argument = argument ?? throw new ArgumentNullException(nameof(argument));
        Kind = kind;
        IsFinal = isFinal;
        IsImplicit = isImplicit;
        HasError = false;
        Error = null;
        Value = value;
        ValueLiteral = valueLiteral ?? throw new ArgumentNullException(nameof(valueLiteral));
    }

    public ArgumentValue(IInputFieldInfo argument, IError error)
    {
        _argument = argument ?? throw new ArgumentNullException(nameof(argument));
        Error = error ?? throw new ArgumentNullException(nameof(error));
        HasError = true;
        IsFinal = true;
        Kind = null;
        Value = null;
        ValueLiteral = null;
    }

    /// <summary>
    /// Gets the argument name.
    /// </summary>
    public NameString Name => _argument.Name;

    /// <summary>
    /// Gets the argument field coordinate.
    /// </summary>
    public FieldCoordinate Coordinate => _argument.Coordinate;

    /// <summary>
    /// Gets the argument type.
    /// </summary>
    public IInputType Type => _argument.Type;

    /// <summary>
    /// Gets the argument runtime type.
    /// </summary>
    public Type RuntimeType => _argument.RuntimeType;

    /// <summary>
    /// Gets the argument default value.
    /// </summary>
    public IValueNode? DefaultValue => _argument.DefaultValue;

    /// <summary>
    /// Return an optional input value formatter.
    /// </summary>
    public IInputValueFormatter? Formatter => _argument.Formatter;

    public ValueKind? Kind { get; }

    /// <summary>
    /// Defines if this argument value is fully coerced and
    /// need no post processing.
    /// </summary>
    public bool IsFinal { get; }

    /// <summary>
    /// Defines if this argument value has errors.
    /// </summary>
    public bool HasError { get; }

    public bool IsImplicit { get; }

    /// <summary>
    /// Gets the runtime value representation of this argument.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets the value literal of this argument value.
    /// </summary>
    public IValueNode? ValueLiteral { get; }

    /// <summary>
    /// If this argument has error this represents the argument error.
    /// </summary>
    public IError? Error { get; }

}
