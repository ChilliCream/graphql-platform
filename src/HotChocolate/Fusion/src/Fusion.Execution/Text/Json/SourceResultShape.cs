using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate.Fusion.Execution.Clients.AliasBatching;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// The shape that the JSON result of a source schema is expected to have: for every object the
/// names of its properties in the order in which they are expected, and the shape of every
/// property value. A result that has another shape is still read correctly.
/// </summary>
internal sealed class SourceResultShape
{
    /// <summary>
    /// The node of a value that has no expected shape.
    /// </summary>
    public const int NoNode = -1;

    /// <summary>
    /// Marks a node whose expected fields are returned under the aliases of a batched request,
    /// which are the field names behind a prefix with the position of the field in the batch.
    /// </summary>
    public const int AliasedFields = -2;

    private const int MaxSelectionSetsPerNode = 8;

    private readonly Node[] _nodes;
    private readonly Field[] _fields;
    private readonly string[] _fieldNames;
    private readonly byte[] _nameTokens;
    private readonly NodeSelections?[] _nodeSelections;

    private SourceResultShape(
        Node[] nodes,
        Field[] fields,
        string[] fieldNames,
        byte[] nameTokens,
        NodeSelections?[] nodeSelections,
        int root)
    {
        _nodes = nodes;
        _fields = fields;
        _fieldNames = fieldNames;
        _nameTokens = nameTokens;
        _nodeSelections = nodeSelections;
        Root = root;
    }

    /// <summary>
    /// Gets the node of the root value of the result.
    /// </summary>
    public int Root { get; }

    /// <summary>
    /// Gets the nodes of the shape. A node describes an object, and also the objects of a list
    /// of any depth that takes the place of that object.
    /// </summary>
    public ReadOnlySpan<Node> Nodes => _nodes;

    /// <summary>
    /// Gets the expected fields of all nodes. The fields of a node lie next to each other.
    /// </summary>
    public ReadOnlySpan<Field> Fields => _fields;

    /// <summary>
    /// Gets the same shape with another root node.
    /// </summary>
    public SourceResultShape WithRoot(int root)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(root);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(root, _nodes.Length);

        return new SourceResultShape(_nodes, _fields, _fieldNames, _nameTokens, _nodeSelections, root);
    }

    /// <summary>
    /// Gets for every expected field of <paramref name="node"/>, in the expected order, the
    /// selection of <paramref name="selectionSet"/> whose response name is the name of the field,
    /// with <c>null</c> for a field that the selection set does not select. Returns <c>null</c>
    /// when the selections of the node are already kept for too many other selection sets.
    /// </summary>
    public Selection?[]? GetSelections(int node, SelectionSet selectionSet)
    {
        for (var current = Volatile.Read(ref _nodeSelections[node]); current is not null; current = current.Next)
        {
            if (ReferenceEquals(current.SelectionSet, selectionSet))
            {
                return current.Selections;
            }
        }

        return AddSelections(node, selectionSet);
    }

    private Selection?[]? AddSelections(int node, SelectionSet selectionSet)
    {
        Selection?[]? selections = null;

        while (true)
        {
            var first = Volatile.Read(ref _nodeSelections[node]);
            var count = 0;

            for (var current = first; current is not null; current = current.Next)
            {
                if (ReferenceEquals(current.SelectionSet, selectionSet))
                {
                    return current.Selections;
                }

                count++;
            }

            // The objects of a node are completed into the selection sets of one operation, one for
            // every object type, so the list is short. Nothing is added to a full list.
            if (count == MaxSelectionSetsPerNode)
            {
                return null;
            }

            if (selections is null)
            {
                var shapeNode = _nodes[node];
                selections = new Selection?[shapeNode.FieldCount];

                for (var i = 0; i < selections.Length; i++)
                {
                    selectionSet.TryGetSelection(_fieldNames[shapeNode.FirstField + i], out selections[i]);
                }
            }

            var added = new NodeSelections(selectionSet, selections, first);

            if (ReferenceEquals(Interlocked.CompareExchange(ref _nodeSelections[node], added, first), first))
            {
                return selections;
            }
        }
    }

    /// <summary>
    /// Determines whether <paramref name="data"/> has the name token of <paramref name="field"/>
    /// at <paramref name="position"/>. The name token is the property name with its quotes,
    /// directly followed by a colon.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWithNameToken(ReadOnlySpan<byte> data, int position, in Field field)
    {
        var length = field.TokenLength;

        if ((uint)position > (uint)data.Length || data.Length - position < Math.Max(length, sizeof(ulong)))
        {
            return StartsWithNameTokenAtEnd(data, position, in field);
        }

        var first = BinaryPrimitives.ReadUInt64LittleEndian(data[position..]);

        if (((first ^ field.FirstBytes) & field.FirstBytesMask) != 0)
        {
            return false;
        }

        if (length <= sizeof(ulong))
        {
            return true;
        }

        if (length <= 2 * sizeof(ulong))
        {
            return BinaryPrimitives.ReadUInt64LittleEndian(data[(position + length - sizeof(ulong))..])
                == field.LastBytes;
        }

        return data.Slice(position + sizeof(ulong), length - sizeof(ulong))
            .SequenceEqual(_nameTokens.AsSpan(field.TokenOffset + sizeof(ulong), length - sizeof(ulong)));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool StartsWithNameTokenAtEnd(ReadOnlySpan<byte> data, int position, in Field field)
        => (uint)position <= (uint)data.Length
            && data[position..].StartsWith(_nameTokens.AsSpan(field.TokenOffset, field.TokenLength));

    /// <summary>
    /// Gets the node of the value of a property whose name token was not found in the input at
    /// the position of the property, for an object with the node <paramref name="node"/>.
    /// </summary>
    /// <param name="node">The node of the object, or <see cref="NoNode"/>.</param>
    /// <param name="name">
    /// The property name with its quotes, or an empty span when the name cannot be compared.
    /// </param>
    /// <param name="nextField">
    /// The position of the next expected field of the object. It is moved behind the field with
    /// the name <paramref name="name"/> when that field is the next one or is expected later.
    /// </param>
    /// <param name="mismatchBits">
    /// The bit set of the objects that do not have exactly the expected fields in the expected
    /// order. The object is added unless <paramref name="name"/> is its next expected field.
    /// </param>
    /// <param name="unknownNameBits">
    /// The bit set of the objects that had a property that none of their later expected fields
    /// names. The later expected fields of such an object are not searched again.
    /// </param>
    /// <param name="depth">The bit of the object in both bit sets.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int ResolveUnexpectedName(
        int node,
        ReadOnlySpan<byte> name,
        ref int nextField,
        ref ulong mismatchBits,
        ref ulong unknownNameBits,
        int depth)
    {
        if (node < 0)
        {
            return NoNode;
        }

        ref readonly var objectNode = ref _nodes[node];

        // Every property of such an object has the same shape and no property is expected by name.
        if (objectNode.AnyFieldChild >= 0)
        {
            return objectNode.AnyFieldChild;
        }

        if (objectNode.AnyFieldChild == AliasedFields)
        {
            return ResolveAliasedField(in objectNode, name);
        }

        // The name token is not found for the next expected field when white space or the end
        // of the read window separates the name from its colon.
        if (name.Length > 0
            && (uint)nextField < (uint)objectNode.FieldCount
            && IsNameOf(in _fields[objectNode.FirstField + nextField], name))
        {
            return _fields[objectNode.FirstField + nextField++].Child;
        }

        var objectBit = 1UL << depth;
        mismatchBits |= objectBit;

        if (name.Length == 0 || (unknownNameBits & objectBit) != 0)
        {
            return NoNode;
        }

        for (var i = nextField + 1; i < objectNode.FieldCount; i++)
        {
            ref readonly var field = ref _fields[objectNode.FirstField + i];

            if (IsNameOf(in field, name))
            {
                nextField = i + 1;
                return field.Child;
            }
        }

        unknownNameBits |= objectBit;
        return NoNode;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsNameOf(in Field field, ReadOnlySpan<byte> quotedName)
        => quotedName.SequenceEqual(_nameTokens.AsSpan(field.TokenOffset, field.TokenLength - 1));

    private int ResolveAliasedField(in Node objectNode, ReadOnlySpan<byte> name)
    {
        if (name.Length < 2 || !AliasBatchPrefixHelper.TryReadIndex(name[1..^1], out _, out var fieldName))
        {
            return NoNode;
        }

        for (var i = 0; i < objectNode.FieldCount; i++)
        {
            ref readonly var field = ref _fields[objectNode.FirstField + i];

            // The name token is the field name in quotes followed by a colon.
            if (fieldName.SequenceEqual(_nameTokens.AsSpan(field.TokenOffset + 1, field.TokenLength - 3)))
            {
                return field.Child;
            }
        }

        return NoNode;
    }

    /// <summary>
    /// An object of the expected shape.
    /// </summary>
    /// <param name="firstField">The position of the first expected field in <see cref="Fields"/>.</param>
    /// <param name="fieldCount">The number of expected fields.</param>
    /// <param name="isIdentified">
    /// Whether an object is identified as an object of this node when it has exactly the expected
    /// fields in the expected order.
    /// </param>
    /// <param name="anyFieldChild">
    /// The node of the value of every property of the object when the property names are not
    /// known in advance, <see cref="AliasedFields"/> when the properties are the expected fields
    /// under the aliases of a batched request, otherwise <see cref="NoNode"/>.
    /// </param>
    internal readonly struct Node(int firstField, int fieldCount, bool isIdentified, int anyFieldChild)
    {
        public readonly int FirstField = firstField;

        public readonly int FieldCount = fieldCount;

        public readonly bool IsIdentified = isIdentified;

        public readonly int AnyFieldChild = anyFieldChild;
    }

    private sealed class NodeSelections(SelectionSet selectionSet, Selection?[] selections, NodeSelections? next)
    {
        public SelectionSet SelectionSet { get; } = selectionSet;

        public Selection?[] Selections { get; } = selections;

        public NodeSelections? Next { get; } = next;
    }

    /// <summary>
    /// An expected field of an object.
    /// </summary>
    internal readonly struct Field
    {
        public Field(ReadOnlySpan<byte> nameToken, int tokenOffset, int child)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];

            buffer.Clear();
            nameToken[..Math.Min(nameToken.Length, sizeof(ulong))].CopyTo(buffer);
            FirstBytes = BinaryPrimitives.ReadUInt64LittleEndian(buffer);
            FirstBytesMask = nameToken.Length >= sizeof(ulong)
                ? ulong.MaxValue
                : (1UL << (nameToken.Length * 8)) - 1;
            LastBytes = nameToken.Length >= sizeof(ulong)
                ? BinaryPrimitives.ReadUInt64LittleEndian(nameToken[^sizeof(ulong)..])
                : 0;
            TokenOffset = tokenOffset;
            TokenLength = nameToken.Length;
            Child = child;
        }

        private Field(Field field, int tokenOffset, int child)
        {
            FirstBytes = field.FirstBytes;
            FirstBytesMask = field.FirstBytesMask;
            LastBytes = field.LastBytes;
            TokenOffset = tokenOffset;
            TokenLength = field.TokenLength;
            Child = child;
        }

        /// <summary>
        /// Gets this field for a shape in which its name token lies at
        /// <paramref name="tokenOffset"/> and its value has the node <paramref name="child"/>.
        /// </summary>
        public Field MoveTo(int tokenOffset, int child) => new(this, tokenOffset, child);

        /// <summary>
        /// The first eight bytes of the name token as a little-endian value, zero padded.
        /// </summary>
        public readonly ulong FirstBytes;

        /// <summary>
        /// The bits of <see cref="FirstBytes"/> that belong to the name token.
        /// </summary>
        public readonly ulong FirstBytesMask;

        /// <summary>
        /// The last eight bytes of a name token of at least eight bytes as a little-endian value.
        /// </summary>
        public readonly ulong LastBytes;

        /// <summary>
        /// The position of the name token in the name tokens of the shape.
        /// </summary>
        public readonly int TokenOffset;

        /// <summary>
        /// The length of the name token in bytes.
        /// </summary>
        public readonly int TokenLength;

        /// <summary>
        /// The node of the value of the field, or <see cref="NoNode"/> when the value has no
        /// expected shape.
        /// </summary>
        public readonly int Child;
    }

    /// <summary>
    /// Builds a <see cref="SourceResultShape"/> from its leaf objects up to its root.
    /// </summary>
    internal sealed class Builder
    {
        private readonly List<Node> _nodes = [];
        private readonly List<Field> _fields = [];
        private readonly List<string> _fieldNames = [];
        private readonly List<byte> _nameTokens = [];

        /// <summary>
        /// Adds an object with the expected fields <paramref name="names"/>, whose values have the
        /// nodes <paramref name="children"/>, and returns its node.
        /// </summary>
        /// <param name="names">The expected property names in the expected order.</param>
        /// <param name="children">The node of every field value, or <see cref="NoNode"/>.</param>
        /// <param name="identify">
        /// Whether the objects that have exactly these fields in this order are identified as
        /// objects of the node. The names must be distinct for such a node.
        /// </param>
        public int AddObject(ReadOnlySpan<string> names, ReadOnlySpan<int> children, bool identify)
        {
            if (names.Length != children.Length)
            {
                throw ThrowHelper.ExpectedFieldsNeedValueNodes();
            }

            var firstField = _fields.Count;
            var distinctNames = identify ? new HashSet<string>(names.Length, StringComparer.Ordinal) : null;

            for (var i = 0; i < names.Length; i++)
            {
                if (distinctNames?.Add(names[i]) == false)
                {
                    throw ThrowHelper.ExpectedFieldNamesAreNotDistinct(names[i]);
                }

                var tokenOffset = _nameTokens.Count;
                var nameToken = CreateNameToken(names[i]);

                _nameTokens.AddRange(nameToken);
                _fields.Add(new Field(nameToken, tokenOffset, children[i]));
                _fieldNames.Add(names[i]);
            }

            _nodes.Add(new Node(firstField, names.Length, identify, NoNode));
            return _nodes.Count - 1;
        }

        /// <summary>
        /// Adds an object whose properties are the expected fields <paramref name="names"/> under
        /// the aliases of a batched request, in any order and number, and returns its node. The
        /// values of the fields have the nodes <paramref name="children"/>.
        /// </summary>
        public int AddObjectOfAliasedFields(ReadOnlySpan<string> names, ReadOnlySpan<int> children)
        {
            var node = AddObject(names, children, identify: false);
            var added = _nodes[node];

            _nodes[node] = new Node(added.FirstField, added.FieldCount, isIdentified: false, AliasedFields);
            return node;
        }

        /// <summary>
        /// Adds all objects of <paramref name="shape"/> and returns the number that is added to
        /// a node of <paramref name="shape"/> to get its node in this builder.
        /// </summary>
        public int Append(SourceResultShape shape)
        {
            ArgumentNullException.ThrowIfNull(shape);

            var nodeOffset = _nodes.Count;
            var fieldOffset = _fields.Count;
            var tokenOffset = _nameTokens.Count;

            foreach (var node in shape._nodes)
            {
                _nodes.Add(
                    new Node(
                        node.FirstField + fieldOffset,
                        node.FieldCount,
                        node.IsIdentified,
                        node.AnyFieldChild >= 0 ? node.AnyFieldChild + nodeOffset : node.AnyFieldChild));
            }

            foreach (var field in shape._fields)
            {
                _fields.Add(
                    field.MoveTo(
                        field.TokenOffset + tokenOffset,
                        field.Child >= 0 ? field.Child + nodeOffset : field.Child));
            }

            _fieldNames.AddRange(shape._fieldNames);
            _nameTokens.AddRange(shape._nameTokens);
            return nodeOffset;
        }

        /// <summary>
        /// Adds an object whose property names are not known in advance and whose property values
        /// all have the node <paramref name="child"/>, and returns its node.
        /// </summary>
        public int AddObjectOfAnyFields(int child)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(child);

            _nodes.Add(new Node(_fields.Count, 0, isIdentified: false, child));
            return _nodes.Count - 1;
        }

        /// <summary>
        /// Creates the shape whose root value has the node <paramref name="root"/>.
        /// </summary>
        public SourceResultShape Build(int root)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(root);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(root, _nodes.Count);

            return new SourceResultShape(
                [.. _nodes],
                [.. _fields],
                [.. _fieldNames],
                [.. _nameTokens],
                new NodeSelections?[_nodes.Count],
                root);
        }

        /// <summary>
        /// Determines whether a property with the name <paramref name="name"/> is written to JSON
        /// without escape sequences, which is required of an expected field.
        /// </summary>
        public static bool IsPlainName(string name)
        {
            if (name.Length == 0)
            {
                return false;
            }

            foreach (var c in name)
            {
                if (!char.IsAsciiLetterOrDigit(c) && c != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private static byte[] CreateNameToken(string name)
        {
            if (!IsPlainName(name))
            {
                throw ThrowHelper.ExpectedFieldNameIsNotPlain(name);
            }

            var nameToken = new byte[name.Length + 3];

            nameToken[0] = (byte)'"';
            Encoding.ASCII.GetBytes(name, nameToken.AsSpan(1));
            nameToken[^2] = (byte)'"';
            nameToken[^1] = (byte)':';
            return nameToken;
        }
    }
}
