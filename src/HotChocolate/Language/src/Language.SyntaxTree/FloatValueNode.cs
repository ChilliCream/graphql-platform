using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HotChocolate.Language.Properties;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// <para>
/// A FloatValue includes either a decimal point (ex. 1.0) or an exponent (ex. 1e50) or
/// both (ex. 6.0221413e23) and may be negative. Like IntValue, it also must not have any
/// leading 0.
/// </para>
/// <para>
/// A FloatValue must not be followed by a Digit. In other words, a FloatValue token is always
/// the longest possible valid sequence. The source characters 1.23 cannot be interpreted as
/// two tokens since 1.2 is followed by the Digit 3.
/// </para>
/// <para>
/// A FloatValue must not be followed by a .. For example, the sequence 1.23.4 cannot
/// be interpreted as two tokens (1.2, 3.4).
/// </para>
/// <para>
/// A FloatValue must not be followed by a NameStart. For example the sequence 0x1.2p3
/// has no valid lexical representation.
/// </para>
/// </summary>
public sealed class FloatValueNode
    : IValueNode<string>
    , IEquatable<FloatValueNode>
    , IFloatValueLiteral
{
    private ReadOnlyMemory<byte> _memory;
    private string? _stringValue;
    private float? _floatValue;
    private double? _doubleValue;
    private decimal? _decimalValue;

    /// <summary>
    /// Initializes a new instance of <see cref="FloatValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public FloatValueNode(double value)
        : this(null, value)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FloatValueNode"/>
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    public FloatValueNode(Location? location, double value)
    {
        Location = location;
        _doubleValue = value;
        Format = FloatFormat.FixedPoint;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FloatValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public FloatValueNode(decimal value)
        : this(null, value)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FloatValueNode"/>
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    public FloatValueNode(Location? location, decimal value)
    {
        Location = location;
        _decimalValue = value;
        Format = FloatFormat.FixedPoint;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FloatValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    /// <param name="format">
    /// The format of the parsed float value.
    /// </param>
    public FloatValueNode(ReadOnlyMemory<byte> value, FloatFormat format)
        : this(null, value, format)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FloatValueNode"/>
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    /// <param name="format">
    /// The format of the parsed float value.
    /// </param>
    public FloatValueNode(Location? location, ReadOnlyMemory<byte> value, FloatFormat format)
    {
        if (value.IsEmpty)
        {
            throw new ArgumentNullException(
                nameof(value),
                Resources.FloatValueNode_ValueEmpty);
        }

        Location = location;
        _memory = value;
        Format = format;
    }

    private FloatValueNode(Location? location, FloatFormat format)
    {
        Location = location;
        Format = format;
    }

    /// <inheritdoc cref="ISyntaxNode" />
    public SyntaxKind Kind => SyntaxKind.FloatValue;

    /// <inheritdoc cref="ISyntaxNode" />
    public Location? Location { get; }

    /// <summary>
    /// Gets the format of the parsed float value.
    /// </summary>
    public FloatFormat Format { get; }

    /// <summary>
    /// The raw parsed string representation of the parsed value node.
    /// </summary>
    public unsafe string Value
    {
        get
        {
            if (_stringValue is null)
            {
                ReadOnlySpan<byte> span = AsSpan();
                fixed (byte* b = span)
                {
                    _stringValue = Encoding.UTF8.GetString(b, span.Length);
                }
            }
            return _stringValue;
        }
    }

    object IValueNode.Value => Value;

    /// <inheritdoc cref="ISyntaxNode" />
    public IEnumerable<ISyntaxNode> GetNodes() => Enumerable.Empty<ISyntaxNode>();

    /// <summary>
    /// Determines whether the specified <see cref="FloatValueNode"/>
    /// is equal to the current <see cref="FloatValueNode"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="FloatValueNode"/> to compare with the current
    /// <see cref="FloatValueNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="FloatValueNode"/> is equal
    /// to the current <see cref="FloatValueNode"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(FloatValueNode? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(other, this))
        {
            return true;
        }

        ReadOnlyMemory<byte> ourMem = AsMemory();
        ReadOnlyMemory<byte> otherMem = other.AsMemory();

        // memory is not doing a deep equality check,
        // but it will be equal if we are referring to the same
        // underlying array.
        if (otherMem.Equals(ourMem))
        {
            return true;
        }

        // if the length is not equals we can do a quick exit.
        if (ourMem.Length != otherMem.Length)
        {
            return false;
        }

        // last we will do a sequence equals and compare the utf8string representation of
        // this value.
        return ourMem.Span.SequenceEqual(otherMem.Span);
    }

    /// <summary>
    /// Determines whether the specified <see cref="IValueNode"/> is equal
    /// to the current <see cref="FloatValueNode"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="IValueNode"/> to compare with the current
    /// <see cref="FloatValueNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="IValueNode"/> is equal
    /// to the current <see cref="FloatValueNode"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(IValueNode? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(other, this))
        {
            return true;
        }

        if (other.GetType() != GetType())
        {
            return false;
        }

        return Equals((FloatValueNode)other);
    }

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to
    /// the current <see cref="FloatValueNode"/>.
    /// </summary>
    /// <param name="obj">
    /// The <see cref="object"/> to compare with the current
    /// <see cref="FloatValueNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="object"/> is equal to the
    /// current <see cref="FloatValueNode"/>; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(obj, this))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((FloatValueNode)obj);
    }

    /// <summary>
    /// Serves as a hash function for a <see cref="FloatValueNode"/>
    /// object.
    /// </summary>
    /// <returns>
    /// A hash code for this instance that is suitable for use in
    /// hashing algorithms and data structures such as a hash table.
    /// </returns>
    public override int GetHashCode()
    {
#if NET6_0_OR_GREATER
        var hashCode = new HashCode();
        hashCode.Add(Kind);
        hashCode.AddBytes(AsSpan());
        return hashCode.ToHashCode();
#else
        var hashCode = new HashCode();
        hashCode.Add(Kind);
        HashCodeExtensions.AddBytes(ref hashCode, AsSpan());
        return hashCode.ToHashCode();
#endif
    }

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    public override string ToString() => SyntaxPrinter.Print(this, true);

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <param name="indented">
    /// A value that indicates whether the GraphQL output should be formatted,
    /// which includes indenting nested GraphQL tokens, adding
    /// new lines, and adding white space between property names and values.
    /// </param>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    public string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

    /// <summary>
    /// Reads the parsed float value as <see cref="float"/>.
    /// </summary>
    public float ToSingle()
    {
        if (_floatValue.HasValue)
        {
            return _floatValue.Value;
        }

        var format = Format == FloatFormat.FixedPoint ? 'g' : 'e';

        if (Utf8Parser.TryParse(AsSpan(), out float value, out _, format))
        {
            _floatValue = value;
            return value;
        }

        throw new InvalidFormatException();
    }

    /// <summary>
    /// Reads the parsed float value as <see cref="double"/>.
    /// </summary>
    public double ToDouble()
    {
        if (_doubleValue.HasValue)
        {
            return _doubleValue.Value;
        }

        var format = Format == FloatFormat.FixedPoint ? 'g' : 'e';

        if (Utf8Parser.TryParse(AsSpan(), out double value, out _, format))
        {
            _doubleValue = value;
            return value;
        }

        throw new InvalidFormatException();
    }

    /// <summary>
    /// Reads the parsed float value as <see cref="decimal"/>.
    /// </summary>
    public decimal ToDecimal()
    {
        if (_decimalValue.HasValue)
        {
            return _decimalValue.Value;
        }

        var format = Format == FloatFormat.FixedPoint ? 'g' : 'e';

        if (Utf8Parser.TryParse(AsSpan(), out decimal value, out _, format))
        {
            _decimalValue = value;
            return value;
        }

        throw new InvalidFormatException();
    }

    /// <summary>
    /// Gets a readonly span to access the float value memory.
    /// </summary>
    public ReadOnlySpan<byte> AsSpan()
        => AsMemory().Span;

    private ReadOnlyMemory<byte> AsMemory()
    {
        if (_memory.IsEmpty)
        {
            Span<byte> buffer = stackalloc byte[32];
            int written;

            if (_floatValue.HasValue)
            {
                Utf8Formatter.TryFormat(_floatValue.Value, buffer, out written, 'g');
            }
            else if (_doubleValue.HasValue)
            {
                Utf8Formatter.TryFormat(_doubleValue.Value, buffer, out written, 'g');
            }
            else
            {
                Utf8Formatter.TryFormat(_decimalValue!.Value, buffer, out written, 'g');
            }

            var memory = new Memory<byte>(new byte[written]);
            buffer.Slice(0, written).CopyTo(memory.Span);
            _memory = memory;
        }

        return _memory;
    }

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Location" /> with <paramref name="location" />.
    /// </summary>
    /// <param name="location">
    /// The location that shall be used to replace the current location.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="location" />.
    /// </returns>
    public FloatValueNode WithLocation(Location? location)
        => new(location, Format)
        {
            _memory = _memory,
            _floatValue = _floatValue,
            _doubleValue = _doubleValue,
            _decimalValue = _decimalValue,
            _stringValue = Value
        };

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Value" /> with <paramref name="value" />.
    /// </summary>
    /// <param name="value">
    /// The value that shall be used to replace the current value.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="value" />.
    /// </returns>
    public FloatValueNode WithValue(double value)
        => new(Location, value);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Value" /> with <paramref name="value" />.
    /// </summary>
    /// <param name="value">
    /// The value that shall be used to replace the current value.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="value" />.
    /// </returns>
    public FloatValueNode WithValue(decimal value)
        => new(Location, value);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Value" /> with <paramref name="value" />.
    /// </summary>
    /// <param name="value">
    /// The value that shall be used to replace the current value.
    /// </param>
    /// <param name="format">
    /// The parsed float format.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="value" />.
    /// </returns>
    public FloatValueNode WithValue(ReadOnlyMemory<byte> value, FloatFormat format)
        => new(Location, value, format);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Value" /> with <paramref name="value" />.
    /// </summary>
    /// <param name="value">
    /// The value that shall be used to replace the current value.
    /// </param>
    /// <param name="format">
    /// The parsed float format.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="value" />.
    /// </returns>
    public FloatValueNode WithValue(ReadOnlySpan<byte> value, FloatFormat format)
        => new(Location, value.ToArray(), format);

    public static bool operator ==(FloatValueNode? left, FloatValueNode? right)
        => Equals(left, right);

    public static bool operator !=(FloatValueNode? left, FloatValueNode? right)
        => !Equals(left, right);
}
