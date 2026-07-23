using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using HotChocolate.Buffers;

namespace HotChocolate.Language;

/// <summary>
/// Represents an executable GraphQL document as a packed, immutable UTF-8 syntax tree over
/// operations and fragments. The document preserves the original source ranges and formats them
/// verbatim, splicing in variable-name substitutions on demand while every other byte is
/// reproduced unchanged.
/// </summary>
public sealed partial class Utf8OperationDocument : IDisposable, IUtf8SyntaxNode
{
    private static readonly Encoding s_utf8 = Encoding.UTF8;
    private readonly ReadOnlyMemorySegment _source;
    private readonly byte[] _sourceBuffer;
    private readonly int _sourceStart;
    private MetaDb _metaDb;
    private VariableTable _variables;
    private int _disposed;

    internal Utf8OperationDocument(
        ReadOnlyMemorySegment source,
        MetaDb metaDb,
        VariableTable variables)
    {
        if (source.IsEmpty)
        {
            throw new ArgumentException("The source must not be empty.", nameof(source));
        }

        _source = source;
        _metaDb = metaDb;
        _variables = variables;

        if (MemoryMarshal.TryGetArray(source.Memory, out var array))
        {
            _sourceBuffer = array.Array!;
            _sourceStart = array.Offset;
        }
        else
        {
            // The owner is not array-backed; copy once so hot reads stay array based.
            _sourceBuffer = source.Memory.ToArray();
            _sourceStart = 0;
        }
    }

    /// <summary>
    /// Gets the operation definitions in this document.
    /// </summary>
    public Utf8OperationDefinitionEnumerable GetOperations()
        => new(this);

    /// <summary>
    /// Gets the fragment definitions in this document.
    /// </summary>
    public Utf8FragmentDefinitionEnumerable GetFragments()
        => new(this);

    /// <summary>
    /// Writes the GraphQL source text of this document to the specified buffer writer,
    /// substituting variable names through <paramref name="variables"/>. Formatting with an
    /// empty map reproduces the original source byte for byte.
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
        if (_disposed != 0)
        {
            throw new ObjectDisposedException(nameof(Utf8OperationDocument));
        }

        Utf8SyntaxFormatter.WriteRange(this, 0, SourceLength, writer, variables);
    }

    internal int RowCount => _metaDb.RowCount;

    internal int SourceLength => _source.Length;

    internal int MetadataLength => _metaDb.Length;

    internal DbRow GetRow(int index) => _metaDb.GetRow(index);

    /// <summary>
    /// Gets the number of distinct variable names recorded in this document.
    /// </summary>
    public int VariableCount => _variables.VariableCount;

    /// <summary>
    /// Gets the number of recorded variable occurrence sites in this document.
    /// </summary>
    internal int VariableSiteCount => _variables.SiteCount;

    /// <summary>
    /// Returns the name, without the leading dollar sign, of the distinct variable identified by
    /// <paramref name="ordinal"/>. Ordinals are document-scoped and assigned by first occurrence
    /// in document order.
    /// </summary>
    public ReadOnlySpan<byte> GetVariableName(int ordinal)
    {
        _variables.GetVariableName(ordinal, out var nameStart, out var length);
        return GetSource(nameStart, length);
    }

    /// <summary>
    /// Returns the source offset of the name token of the site at <paramref name="index"/>.
    /// </summary>
    internal int GetVariableSitePosition(int index) => _variables.GetSitePosition(index);

    /// <summary>
    /// Returns the ordinal of the distinct variable that the site at <paramref name="index"/>
    /// refers to.
    /// </summary>
    internal int GetVariableSiteOrdinal(int index) => _variables.GetSiteOrdinal(index);

    /// <summary>
    /// Returns the index of the first variable site whose name-token position is at or after
    /// <paramref name="position"/>, or <see cref="VariableSiteCount"/> when no such site exists.
    /// </summary>
    internal int FindFirstVariableSite(int position) => _variables.FindFirstSiteAtOrAfter(position);

    /// <summary>
    /// Returns the index of the first row at or after <paramref name="start"/> that is not a
    /// variable definition, hopping over each variable definition and the rows it spans.
    /// </summary>
    internal int SkipVariableDefinitions(int start)
    {
        var index = start;
        var rowCount = RowCount;

        while (index < rowCount)
        {
            var row = GetRow(index);
            if (row.Kind is not Utf8SyntaxKind.VariableDefinition)
            {
                break;
            }

            index += row.NumberOfRows;
        }

        return index;
    }

    internal ReadOnlySpan<byte> GetSource(int start, int length)
    {
        if (start < 0 || length < 0 || start > _source.Length - length)
        {
            throw new InvalidOperationException("The syntax node contains an invalid source range.");
        }

        return _sourceBuffer.AsSpan(_sourceStart + start, length);
    }

    internal string GetString(int start, int length)
    {
#if NETSTANDARD2_0
        return s_utf8.GetString(_sourceBuffer, _sourceStart + start, length);
#else
        return s_utf8.GetString(GetSource(start, length));
#endif
    }

    /// <summary>
    /// Releases pooled resources held by this document. Any further use of the document
    /// is invalid.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _metaDb.Dispose();
        _variables.Dispose();
    }
}
