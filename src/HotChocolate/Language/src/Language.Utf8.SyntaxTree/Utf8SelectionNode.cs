using System.Buffers;
using System.Diagnostics;

namespace HotChocolate.Language;

/// <summary>
/// Specifies the kind of executable selection represented by a <see cref="Utf8SelectionNode"/>.
/// </summary>
public enum Utf8SelectionKind
{
    /// <summary>
    /// The selection is not initialized.
    /// </summary>
    None = 0,

    /// <summary>
    /// The selection is a field.
    /// </summary>
    Field = 1,

    /// <summary>
    /// The selection is a fragment spread.
    /// </summary>
    FragmentSpread = 2,

    /// <summary>
    /// The selection is an inline fragment.
    /// </summary>
    InlineFragment = 3
}

/// <summary>
/// Provides a heterogeneous view over an executable selection in a packed UTF-8 syntax tree.
/// </summary>
public readonly struct Utf8SelectionNode : IUtf8SyntaxNode
{
    private readonly Utf8OperationDocument _document;
    private readonly int _cursor;

    internal Utf8SelectionNode(Utf8OperationDocument document, int cursor)
    {
        // document is usually not null, but the Current property on the enumerators
        // (when initialized as default) can physically pass null.
        _document = document;
        _cursor = cursor;
        Debug.Assert(document.GetRow(cursor).Kind
            is Utf8SyntaxKind.Field
            or Utf8SyntaxKind.FragmentSpread
            or Utf8SyntaxKind.InlineFragment);
    }

    /// <summary>
    /// Gets the selection kind.
    /// </summary>
    public Utf8SelectionKind Kind => _document is null
        ? Utf8SelectionKind.None
        : _document.GetRow(_cursor).Kind switch
        {
            Utf8SyntaxKind.Field => Utf8SelectionKind.Field,
            Utf8SyntaxKind.FragmentSpread => Utf8SelectionKind.FragmentSpread,
            Utf8SyntaxKind.InlineFragment => Utf8SelectionKind.InlineFragment,
            _ => Utf8SelectionKind.None
        };

    /// <summary>
    /// Gets this selection as a field.
    /// </summary>
    public Utf8FieldNode GetField()
        => Kind is Utf8SelectionKind.Field
            ? new Utf8FieldNode(_document, _cursor)
            : throw new InvalidOperationException("The selection is not a field.");

    /// <summary>
    /// Gets this selection as a fragment spread.
    /// </summary>
    public Utf8FragmentSpreadNode GetFragmentSpread()
        => Kind is Utf8SelectionKind.FragmentSpread
            ? new Utf8FragmentSpreadNode(_document, _cursor)
            : throw new InvalidOperationException("The selection is not a fragment spread.");

    /// <summary>
    /// Gets this selection as an inline fragment.
    /// </summary>
    public Utf8InlineFragmentNode GetInlineFragment()
        => Kind is Utf8SelectionKind.InlineFragment
            ? new Utf8InlineFragmentNode(_document, _cursor)
            : throw new InvalidOperationException("The selection is not an inline fragment.");

    /// <summary>
    /// Tries to get this selection as a field.
    /// </summary>
    public bool TryGetField(out Utf8FieldNode field)
    {
        if (Kind is Utf8SelectionKind.Field)
        {
            field = new Utf8FieldNode(_document, _cursor);
            return true;
        }

        field = default;
        return false;
    }

    /// <summary>
    /// Tries to get this selection as a fragment spread.
    /// </summary>
    public bool TryGetFragmentSpread(out Utf8FragmentSpreadNode fragmentSpread)
    {
        if (Kind is Utf8SelectionKind.FragmentSpread)
        {
            fragmentSpread = new Utf8FragmentSpreadNode(_document, _cursor);
            return true;
        }

        fragmentSpread = default;
        return false;
    }

    /// <summary>
    /// Tries to get this selection as an inline fragment.
    /// </summary>
    public bool TryGetInlineFragment(out Utf8InlineFragmentNode inlineFragment)
    {
        if (Kind is Utf8SelectionKind.InlineFragment)
        {
            inlineFragment = new Utf8InlineFragmentNode(_document, _cursor);
            return true;
        }

        inlineFragment = default;
        return false;
    }

    /// <summary>
    /// Writes this selection's GraphQL source text to the specified buffer writer, substituting
    /// variable names through <paramref name="variables"/>.
    /// </summary>
    /// <param name="writer">
    /// The buffer writer that receives the UTF-8 encoded output.
    /// </param>
    /// <param name="variables">
    /// The ordinal-indexed variable name substitutions to apply, or the default value to keep
    /// every original name.
    /// </param>
    public void Format(IBufferWriter<byte> writer, Utf8VariableNameMap variables = default)
    {
        CheckValidInstance();
        Utf8SyntaxFormatter.Write(_document, _cursor, writer, variables);
    }

    private void CheckValidInstance()
    {
        if (_document is null)
        {
            throw new InvalidOperationException();
        }
    }
}
