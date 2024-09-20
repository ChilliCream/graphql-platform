using HotChocolate.Language;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Resolvers;

/// <summary>
/// Represents an argument value withing the field execution pipeline.
/// </summary>
public sealed class ArgumentValue : IInputFieldInfo
{
    private readonly IInputFieldInfo _argument;

    /// <summary>
    /// Initializes a new instance of <see cref="ArgumentValue"/>.
    /// </summary>
    /// <param name="argument">The argument.</param>
    /// <param name="kind">
    /// The value kind.
    /// </param>
    /// <param name="isFullyCoerced">
    /// Specifies if this value is final or if it needs to be coerced during field execution.
    /// Values with variables for instance need coercion during field execution.
    /// </param>
    /// <param name="isDefaultValue">
    /// Defines if the provided value represents the argument default value and was not explicitly
    /// provided by the user.
    /// </param>
    /// <param name="value">
    /// The runtime value representation.
    /// </param>
    /// <param name="valueLiteral">
    /// The syntax value representation.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="argument"/> or <paramref name="valueLiteral" /> is <c>null</c>.
    /// </exception>
    public ArgumentValue(
        IInputFieldInfo argument,
        ValueKind kind,
        bool isFullyCoerced,
        bool isDefaultValue,
        object? value,
        IValueNode valueLiteral)
    {
        _argument = argument ?? throw new ArgumentNullException(nameof(argument));
        Kind = kind;
        IsDefaultValue = isDefaultValue;
        IsFullyCoerced = isFullyCoerced;
        HasError = false;
        Error = null;
        Value = value;
        ValueLiteral = valueLiteral ?? throw new ArgumentNullException(nameof(valueLiteral));
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ArgumentValue"/>.
    /// </summary>
    /// <param name="argument">The argument.</param>
    /// <param name="error">The argument value error.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="argument"/> or <paramref name="error" /> is <c>null</c>.
    /// </exception>
    public ArgumentValue(IInputFieldInfo argument, IError error)
    {
        _argument = argument ?? throw new ArgumentNullException(nameof(argument));
        Error = error ?? throw new ArgumentNullException(nameof(error));
        HasError = true;
        IsFullyCoerced = true;
        Kind = null;
        Value = null;
        ValueLiteral = null;
    }

    /// <summary>
    /// Gets the argument name.
    /// </summary>
    public string Name => _argument.Name;

    /// <summary>
    /// Gets the argument schema coordinate.
    /// </summary>
    public SchemaCoordinate Coordinate => _argument.Coordinate;

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

    /// <summary>
    /// Specifies the value structure.
    /// </summary>
    public ValueKind? Kind { get; }

    /// <summary>
    /// Defines if this argument value is fully coerced and
    /// needs no post processing.
    /// </summary>
    public bool IsFullyCoerced { get; }

    /// <summary>
    /// Defines if this argument value has errors that will
    /// be thrown during field execution.
    /// </summary>
    public bool HasError { get; }

    /// <summary>
    /// Defines if the value was inferred from the default value.
    /// </summary>
    public bool IsDefaultValue { get; }

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
