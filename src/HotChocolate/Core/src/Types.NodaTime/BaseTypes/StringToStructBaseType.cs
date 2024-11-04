using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using NodaTime.Text;
using static HotChocolate.Types.NodaTime.Properties.NodaTimeResources;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// This base class provides serialization functionality for noda time scalars
/// that have a <see cref="string"/> result value and a struct runtime value.
/// </summary>
/// <typeparam name="TRuntimeType">
/// The runtime type.
/// </typeparam>
public abstract class StringToStructBaseType<TRuntimeType>
    : ScalarType<TRuntimeType, StringValueNode>
    where TRuntimeType : struct
{
    /// <summary>
    /// Initializes a new instance of <see cref="StringToStructBaseType{TRuntimeType}"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the scalar.
    /// </param>
    public StringToStructBaseType(string name)
        : base(name, BindingBehavior.Implicit)
    {
    }

    /// <inheritdoc />
    protected override TRuntimeType ParseLiteral(StringValueNode literal)
    {
        if (TryDeserialize(literal.Value, out var value))
        {
            return value.Value;
        }

        throw new SerializationException(
            string.Format(StringToStructBaseType_ParseLiteral_UnableToDeserializeString, Name),
            this);
    }

    /// <inheritdoc />
    protected override StringValueNode ParseValue(TRuntimeType value)
    {
        return new(Serialize(value));
    }

    /// <inheritdoc />
    public override IValueNode ParseResult(object? resultValue)
    {
        if (resultValue is null)
        {
            return NullValueNode.Default;
        }

        if (resultValue is string s)
        {
            return new StringValueNode(s);
        }

        if (resultValue is TRuntimeType v)
        {
            return ParseValue(v);
        }

        throw new SerializationException(
            string.Format(StringToStructBaseType_ParseLiteral_UnableToDeserializeString, Name),
            this);
    }

    /// <inheritdoc />
    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
    {
        if (runtimeValue is null)
        {
            resultValue = null;
            return true;
        }

        if (runtimeValue is TRuntimeType dt)
        {
            resultValue = Serialize(dt);
            return true;
        }

        resultValue = null;
        return false;
    }

    /// <summary>
    /// Serializes the .net runtime representation to the serialized result representation.
    /// </summary>
    /// <param name="runtimeValue">
    /// The .net value representation.
    /// </param>
    /// <returns>
    /// Returns the serialized result value.
    /// </returns>
    protected abstract string Serialize(TRuntimeType runtimeValue);

    /// <inheritdoc />
    public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
    {
        if (resultValue is null)
        {
            runtimeValue = null;
            return true;
        }

        if (resultValue is string s && TryDeserialize(s, out var val))
        {
            runtimeValue = val;
            return true;
        }

        runtimeValue = null;
        return false;
    }

    /// <summary>
    /// Tries to deserializes the value from the output format to the .net
    /// runtime representation.
    /// </summary>
    /// <param name="resultValue">
    /// The serialized result value.
    /// </param>
    /// <param name="runtimeValue">
    /// The .net runtime representation.
    /// </param>
    /// <returns>
    /// <c>true</c> if the serialized value was correctly deserialized; otherwise, <c>false</c>.
    /// </returns>
    protected abstract bool TryDeserialize(
        string resultValue,
        [NotNullWhen(true)] out TRuntimeType? runtimeValue);

    protected string CreateDescription(
        IPattern<TRuntimeType>[] allowedPatterns,
        string description,
        string extendedDescription)
    {
        if (allowedPatterns.All(PatternMap.ContainsKey))
        {
            var patternsText =
                string.Join("\n", allowedPatterns.Select(p => $"- `{PatternMap[p]}`"));
            var examplesText =
                string.Join("\n", allowedPatterns.Select(e => $"- `{ExampleMap[e]}`"));

            return string.Format(extendedDescription, patternsText, examplesText);
        }

        return description;
    }

    /// <summary>
    /// A map from Noda Time patterns to more universal (ISO-like) formats for display purposes.
    /// </summary>
    protected abstract Dictionary<IPattern<TRuntimeType>, string> PatternMap { get; }

    /// <summary>
    /// A map from Noda Time patterns to example strings.
    /// </summary>
    protected abstract Dictionary<IPattern<TRuntimeType>, string> ExampleMap { get; }
}
