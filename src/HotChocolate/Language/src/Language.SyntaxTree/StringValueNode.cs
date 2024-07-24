using System.Text;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// Represents a string value literal.
/// http://facebook.github.io/graphql/June2018/#sec-String-Value
/// </summary>
public sealed class StringValueNode : IValueNode<string>, IHasSpan
{
    private ReadOnlyMemory<byte> _memory;
    private string? _value;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="StringValueNode"/> class.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value"/> is <c>null</c>.
    /// </exception>
    public StringValueNode(string value)
        : this(null, value, false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="StringValueNode"/> class.
    /// </summary>
    /// <param name="location">The source location.</param>
    /// <param name="value">The string value.</param>
    /// <param name="block">
    /// If set to <c>true</c> this instance represents a block string.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value"/> is <c>null</c>.
    /// </exception>
    public StringValueNode(
        Location? location,
        string value,
        bool block)
    {
        Location = location;
        _value = value ?? throw new ArgumentNullException(nameof(value));
        Block = block;
    }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="StringValueNode"/> class.
    /// </summary>
    /// <param name="location">The source location.</param>
    /// <param name="value">The string value.</param>
    /// <param name="block">
    /// If set to <c>true</c> this instance represents a block string.
    /// </param>
    public StringValueNode(
        Location? location,
        ReadOnlyMemory<byte> value,
        bool block)
    {
        Location = location;
        _memory = value;
        Block = block;
    }

    /// <inheritdoc cref="ISyntaxNode"/>
    public SyntaxKind Kind => SyntaxKind.StringValue;

    /// <inheritdoc cref="ISyntaxNode"/>
    public Location? Location { get; }

    public unsafe string Value
    {
        get
        {
            if (_value is null)
            {
                var span = AsSpan();
                fixed (byte* b = span)
                {
                    _value = Encoding.UTF8.GetString(b, span.Length);
                }
            }
            return _value;
        }
    }

    object IValueNode.Value => Value;

    /// <summary>
    /// Gets a value indicating whether this <see cref="StringValueNode"/>
    /// was parsed from a block string.
    /// </summary>
    /// <value>
    /// <c>true</c> if this string value was parsed from a block string;
    /// otherwise, <c>false</c>.
    /// </value>
    public bool Block { get; }

    /// <inheritdoc cref="ISyntaxNode"/>
    public IEnumerable<ISyntaxNode> GetNodes() => [];

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

    internal ReadOnlyMemory<byte> AsMemory()
    {
        if (_memory.IsEmpty)
        {
            _memory = Encoding.UTF8.GetBytes(_value!);
        }
        return _memory;
    }

    internal bool IsMemory => _memory.IsEmpty;

    /// <summary>
    /// Gets a readonly span to access the string value memory.
    /// </summary>
    public ReadOnlySpan<byte> AsSpan() => AsMemory().Span;

    public StringValueNode WithLocation(Location? location)
        => new(location, Value, Block);

    public StringValueNode WithValue(string value)
        => new(Location, value, false);

    public StringValueNode WithValue(string value, bool block)
        => new(Location, value, block);
}
