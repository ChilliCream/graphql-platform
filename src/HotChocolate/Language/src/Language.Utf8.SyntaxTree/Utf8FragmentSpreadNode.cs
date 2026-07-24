using System.Buffers;
using System.Diagnostics;

namespace HotChocolate.Language;

/// <summary>
/// Provides a view over a fragment spread in a packed UTF-8 syntax tree.
/// </summary>
public readonly struct Utf8FragmentSpreadNode : IUtf8SyntaxNode
{
    private readonly Utf8OperationDocument _document;
    private readonly int _cursor;

    internal Utf8FragmentSpreadNode(Utf8OperationDocument document, int cursor)
    {
        // document is usually not null, but the Current property on the enumerators
        // (when initialized as default) can physically pass null.
        _document = document;
        _cursor = cursor;
        Debug.Assert(document.GetRow(cursor).Kind is Utf8SyntaxKind.FragmentSpread);
    }

    /// <summary>
    /// Gets the referenced fragment name.
    /// </summary>
    internal string Name
    {
        get
        {
            CheckValidInstance();
            var nameRow = _document.GetRow(_cursor + 1);
            return _document.GetString(nameRow.Location, nameRow.SizeOrLength);
        }
    }

    /// <summary>
    /// Gets the UTF-8 encoded referenced fragment name.
    /// </summary>
    public ReadOnlySpan<byte> Utf8Name
    {
        get
        {
            CheckValidInstance();
            var nameRow = _document.GetRow(_cursor + 1);
            return _document.GetSource(nameRow.Location, nameRow.SizeOrLength);
        }
    }

    /// <summary>
    /// Writes this fragment spread's GraphQL source text to the specified buffer writer,
    /// substituting variable names through <paramref name="variables"/>.
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
