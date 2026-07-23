using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using HotChocolate.Buffers;

namespace HotChocolate.Language;

public class Utf8GraphQLOperationParserTests
{
    [Fact]
    public void ParseOperationDocument_Should_ExposePackedExecutableStructure_When_DocumentIsComplex()
    {
        // arrange
        const string source = """
            query Search($term: String! = "hot" @tag) @operation {
              result: search(term: $term) {
                title
                ...Card
                ... on Product @include(if: true) { price }
                ... @defer { id }
              }
            }
            fragment Card($size: Int) on Item @fragment { id }
            """;

        // act
        var document = Utf8GraphQLOperationParser.Parse(
            Encoding.UTF8.GetBytes(source),
            new ParserOptions(allowFragmentVariables: true));

        // assert
        Assert.Equal(
            """
            operation Query Search
              variable term
              field result:search
                field title
                spread Card
                inline Product
                  field price
                inline
                  field id
            fragment Card on Item
              variable size
              field id

            """,
            Describe(document));
    }

    [Fact]
    public void ParseOperationDocument_Should_PreserveOffsets_When_SourceHasBomCommentsAndUtf8Strings()
    {
        // arrange
        var query = Encoding.UTF8.GetBytes("query Q { first(text: \"Grüße\") # 日本語\n second }");
        var bom = Encoding.UTF8.GetPreamble();
        var source = new byte[bom.Length + query.Length];
        bom.CopyTo(source, 0);
        query.CopyTo(source, bom.Length);

        // act
        var operation = First(
            Utf8GraphQLOperationParser.Parse(source).GetOperations());
        var selections = operation.SelectionSet.GetSelections().GetEnumerator();
        selections.MoveNext();
        var first = selections.Current.GetField();
        selections.MoveNext();
        var second = selections.Current.GetField();

        // assert
        Assert.Equal("first", Encoding.UTF8.GetString(first.Utf8Name));
        Assert.Equal("second", Encoding.UTF8.GetString(second.Utf8Name));
    }

    [Fact]
    public void ParseOperationDocument_Should_CrossMetadataChunkBoundary_When_DocumentHasManyFields()
    {
        // arrange
        var source = new StringBuilder("{");
        for (var i = 0; i < 3300; i++)
        {
            source.Append(" f").Append(i);
        }
        source.Append(" }");

        // act
        var operation = First(
            Utf8GraphQLOperationParser.Parse(
                Encoding.UTF8.GetBytes(source.ToString()),
                new ParserOptions(maxAllowedFields: 4000)).GetOperations());
        var fields = operation.SelectionSet.GetSelections().GetEnumerator();
        var count = 0;
        var last = string.Empty;
        while (fields.MoveNext())
        {
            count++;
            last = fields.Current.GetField().Name;
        }

        // assert
        Assert.Equal(3300, count);
        Assert.Equal("f3299", last);
    }

    [Fact]
    public void ParseOperationDocument_Should_EnforceFieldLimit_When_LimitIsExceeded()
    {
        // act
        var exception = Record.Exception(
            () => Utf8GraphQLOperationParser.Parse(
                Encoding.UTF8.GetBytes("{ first second }"),
                new ParserOptions(maxAllowedFields: 1)));

        // assert
        Assert.IsType<SyntaxException>(exception);
    }

    [Fact]
    public void SelectionNode_Should_UseConsistentDefaultAndWrongKindBehavior()
    {
        // arrange
        var selection = First(
            First(
                Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes("{ field }"))
                    .GetOperations())
                .SelectionSet
                .GetSelections());
        var defaultSelection = default(Utf8SelectionNode);

        // act
        var wrongKind = Record.Exception(() => selection.GetFragmentSpread());
        var found = defaultSelection.TryGetField(out var field);

        // assert
        Assert.Equal(Utf8SelectionKind.None, defaultSelection.Kind);
        Assert.IsType<InvalidOperationException>(wrongKind);
        Assert.False(found);
        Assert.Equal(default, field);
    }

    [Theory]
    [InlineData("{ field }")]
    [InlineData("query { field }")]
    public void ParseOperationDocument_Should_ExposeAnonymousQuery_When_NameIsOmitted(string source)
    {
        // act
        var operation = First(
            Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes(source)).GetOperations());

        // assert
        Assert.Equal(OperationType.Query, operation.Operation);
        Assert.False(operation.HasName);
        Assert.Null(operation.Name);
        Assert.True(operation.Utf8Name.IsEmpty);
    }

    [Fact]
    public void ParseOperationDocument_Should_RejectSchemaDefinition_When_DocumentIsNotExecutable()
    {
        // act
        var exception = Record.Exception(
            () => Utf8GraphQLOperationParser.Parse(
                Encoding.UTF8.GetBytes("type Query { field: String }")));

        // assert
        Assert.IsType<SyntaxException>(exception);
    }

    [Fact]
    public void ParseOperationDocument_Should_HonorFragmentVariableOption_When_VariablesAreDeclared()
    {
        // arrange
        const string source = "fragment F($id: ID!) on Node { id }";

        // act
        var disabled = Record.Exception(
            () => Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes(source)));
        var enabled = Utf8GraphQLOperationParser.Parse(
            Encoding.UTF8.GetBytes(source),
            new ParserOptions(allowFragmentVariables: true));
        var fragment = First(enabled.GetFragments());
        var variables = fragment.GetVariableDefinitions().GetEnumerator();

        // assert
        Assert.IsType<SyntaxException>(disabled);
        Assert.True(variables.MoveNext());
        Assert.Equal("id", variables.Current.Name);
        Assert.False(variables.MoveNext());
    }

    [Fact]
    public void ParseOperationDocument_Should_RetainOnlyExactSourceArray()
    {
        // arrange
        const string source = "{ one { two } ...F } fragment F on Type { three }";

        // act
        var document = Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes(source));
        var arrayFields = typeof(Utf8OperationDocumentNode)
            .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Where(t => t.FieldType.IsArray)
            .Select(t => t.FieldType)
            .ToArray();

        // assert
        Assert.Equal(Encoding.UTF8.GetByteCount(source), document.SourceLength);
        Assert.Equal(10 * Utf8OperationDocumentNode.DbRow.Size, document.MetadataLength);
        Assert.Equal([typeof(byte[])], arrayFields);
    }

    [Theory]
    [InlineData("query Q($id: [ID!]! = [\"a\", \"b\"]) @one { node(id: $id) @two { id } }")]
    [InlineData("{ field(arg: { one: [1, $value] }) @one }")]
    [InlineData("fragment F on Item { value } query Q { ...F ... on Item { nested } }")]
    public void ParseOperationDocument_Should_MatchClassicNodeLimit_When_LimitIsExact(string source)
    {
        // arrange
        var sourceBytes = Encoding.UTF8.GetBytes(source);
        var classicParser = new Utf8GraphQLParser(sourceBytes);
        classicParser.Parse();
        var exactNodeCount = classicParser.ParsedSyntaxNodes;
        var exact = new ParserOptions(maxAllowedNodes: exactNodeCount);
        var below = new ParserOptions(maxAllowedNodes: exactNodeCount - 1);

        // act
        var classicExact = Record.Exception(() => Utf8GraphQLParser.Parse(sourceBytes, exact));
        var packedExact = Record.Exception(
            () => Utf8GraphQLOperationParser.Parse(sourceBytes, exact));
        var classicBelow = Record.Exception(() => Utf8GraphQLParser.Parse(sourceBytes, below));
        var packedBelow = Record.Exception(
            () => Utf8GraphQLOperationParser.Parse(sourceBytes, below));

        // assert
        Assert.Null(classicExact);
        Assert.Null(packedExact);
        Assert.IsType<SyntaxException>(classicBelow);
        Assert.IsType<SyntaxException>(packedBelow);
    }

    [Fact]
    public void ParseOperationDocument_Should_MatchClassicTokenLimit_When_LimitIsExact()
    {
        // arrange
        const string source = "query Q($id: ID!) { node(id: $id) { id } }";
        var sourceBytes = Encoding.UTF8.GetBytes(source);
        var reader = new Utf8GraphQLReader(sourceBytes);
        var exactTokenCount = reader.Count();
        var exact = new ParserOptions(maxAllowedTokens: exactTokenCount);
        var below = new ParserOptions(maxAllowedTokens: exactTokenCount - 1);

        // act
        var classicExact = Record.Exception(() => Utf8GraphQLParser.Parse(sourceBytes, exact));
        var packedExact = Record.Exception(
            () => Utf8GraphQLOperationParser.Parse(sourceBytes, exact));
        var classicBelow = Record.Exception(() => Utf8GraphQLParser.Parse(sourceBytes, below));
        var packedBelow = Record.Exception(
            () => Utf8GraphQLOperationParser.Parse(sourceBytes, below));

        // assert
        Assert.Null(classicExact);
        Assert.Null(packedExact);
        Assert.IsType<SyntaxException>(classicBelow);
        Assert.IsType<SyntaxException>(packedBelow);
    }

    [Fact]
    public void ParseOperationDocument_Should_MatchClassicDirectiveLimit_When_LimitIsExact()
    {
        // arrange
        const string source = "{ field @one @two }";
        var sourceBytes = Encoding.UTF8.GetBytes(source);
        var exact = new ParserOptions(maxAllowedDirectives: 2);
        var below = new ParserOptions(maxAllowedDirectives: 1);

        // act
        var classicExact = Record.Exception(() => Utf8GraphQLParser.Parse(sourceBytes, exact));
        var packedExact = Record.Exception(
            () => Utf8GraphQLOperationParser.Parse(sourceBytes, exact));
        var classicBelow = Record.Exception(() => Utf8GraphQLParser.Parse(sourceBytes, below));
        var packedBelow = Record.Exception(
            () => Utf8GraphQLOperationParser.Parse(sourceBytes, below));

        // assert
        Assert.Null(classicExact);
        Assert.Null(packedExact);
        Assert.IsType<SyntaxException>(classicBelow);
        Assert.IsType<SyntaxException>(packedBelow);
    }

    [Fact]
    public void ParseOperationDocument_Should_MatchClassicTypeDepthLimit_When_LimitIsExact()
    {
        // arrange
        const string source = "query Q($value: [[[[[Int]]]]]) { field }";
        var sourceBytes = Encoding.UTF8.GetBytes(source);
        var exact = new ParserOptions(maxAllowedRecursionDepth: 6);
        var below = new ParserOptions(maxAllowedRecursionDepth: 5);

        // act
        var classicExact = Record.Exception(() => Utf8GraphQLParser.Parse(sourceBytes, exact));
        var packedExact = Record.Exception(
            () => Utf8GraphQLOperationParser.Parse(sourceBytes, exact));
        var classicBelow = Record.Exception(() => Utf8GraphQLParser.Parse(sourceBytes, below));
        var packedBelow = Record.Exception(
            () => Utf8GraphQLOperationParser.Parse(sourceBytes, below));

        // assert
        Assert.Null(classicExact);
        Assert.Null(packedExact);
        Assert.IsType<SyntaxException>(classicBelow);
        Assert.IsType<SyntaxException>(packedBelow);
    }

    [Theory]
    [InlineData("{ field(arg: \"\\u12G4\") }")]
    [InlineData("{ field(arg: \"\\uD800\") }")]
    public void ParseOperationDocument_Should_RejectInvalidStringEscapeLikeClassicParser(string source)
    {
        // arrange
        var sourceBytes = Encoding.UTF8.GetBytes(source);

        // act
        var classic = Record.Exception(() => Utf8GraphQLParser.Parse(sourceBytes));
        var packed = Record.Exception(
            () => Utf8GraphQLOperationParser.Parse(sourceBytes));

        // assert
        Assert.IsType<Utf8EncodingException>(classic);
        Assert.IsType<Utf8EncodingException>(packed);
    }

    [Theory]
    [InlineData("\"\\u12G4\" query Q { field }")]
    [InlineData("\"\\uD800\" query Q { field }")]
    public void ParseOperationDocument_Should_RejectInvalidDescriptionEscapeLikeClassicParser(
        string source)
    {
        // arrange
        var sourceBytes = Encoding.UTF8.GetBytes(source);

        // act
        var classic = Record.Exception(() => Utf8GraphQLParser.Parse(sourceBytes));
        var packed = Record.Exception(
            () => Utf8GraphQLOperationParser.Parse(sourceBytes));

        // assert
        Assert.IsType<Utf8EncodingException>(classic);
        Assert.IsType<Utf8EncodingException>(packed);
    }

    [Theory]
    [InlineData("query Q($value: String = \"abc\") { field }", "\"abc\"")]
    [InlineData("query Q($value: String = \"\"\"abc\"\"\") { field }", "\"\"\"abc\"\"\"")]
    public void ParseOperationDocument_Should_PreserveExclusiveRawEnd_When_DefaultIsString(
        string source,
        string value)
    {
        // act
        var document = Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes(source));
        var variable = document.GetRow(1);

        // assert
        Assert.Equal(source.IndexOf(value, StringComparison.Ordinal) + value.Length, variable.SourceEnd);
    }

    [Fact]
    public void Enumerables_Should_FindDefinitions_When_FragmentPrecedesOperation()
    {
        // act
        var document = Utf8GraphQLOperationParser.Parse(
            Encoding.UTF8.GetBytes("fragment F on Item { value } query Q { ...F }"));

        // assert
        Assert.Equal("F", First(document.GetFragments()).Name);
        Assert.Equal("Q", First(document.GetOperations()).Name);
    }

    [Fact]
    public void SelectionEnumerable_Should_AdvanceToSibling_When_PreviousFieldHasSubtree()
    {
        // arrange
        var operation = First(
            Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes("{ parent { child } sibling }"))
                .GetOperations());

        // act
        var selections = operation.SelectionSet.GetSelections().GetEnumerator();
        selections.MoveNext();
        var parent = selections.Current.GetField();
        selections.MoveNext();
        var sibling = selections.Current.GetField();

        // assert
        Assert.Equal("child", First(parent.SelectionSet.GetSelections()).GetField().Name);
        Assert.Equal("sibling", sibling.Name);
        Assert.False(selections.MoveNext());
    }

    [Fact]
    public void DefaultViews_Should_ThrowInvalidOperation_When_SelectionSetAccessed()
    {
        // act
        var operationSelection = Record.Exception(
            () => _ = default(Utf8OperationDefinitionNode).SelectionSet);
        var fragmentSelection = Record.Exception(
            () => _ = default(Utf8FragmentDefinitionNode).SelectionSet);
        var inlineSelection = Record.Exception(
            () => _ = default(Utf8InlineFragmentNode).SelectionSet);
        var fieldSelection = Record.Exception(
            () => _ = default(Utf8FieldNode).SelectionSet);
        var selectionSet = Record.Exception(
            () => default(Utf8SelectionSetNode).GetSelections());

        // assert
        Assert.IsType<InvalidOperationException>(operationSelection);
        Assert.IsType<InvalidOperationException>(fragmentSelection);
        Assert.IsType<InvalidOperationException>(inlineSelection);
        Assert.IsType<InvalidOperationException>(fieldSelection);
        Assert.IsType<InvalidOperationException>(selectionSet);
    }

    [Fact]
    public void DefaultViews_Should_ThrowInvalidOperation_When_NameAccessed()
    {
        // act
        var variable = Record.Exception(
            () => _ = default(Utf8VariableDefinitionNode).Name);
        var spread = Record.Exception(
            () => _ = default(Utf8FragmentSpreadNode).Name);

        // assert
        Assert.IsType<InvalidOperationException>(variable);
        Assert.IsType<InvalidOperationException>(spread);
    }

    [Fact]
    public void DefaultDefinitionEnumerables_Should_BeEmpty_When_DocumentIsDefault()
    {
        // act
        var operations = default(Utf8OperationDefinitionEnumerable).GetEnumerator();
        var fragments = default(Utf8FragmentDefinitionEnumerable).GetEnumerator();

        // assert
        Assert.False(operations.MoveNext());
        Assert.False(fragments.MoveNext());
        Assert.Equal(default, operations.Current);
        Assert.Equal(default, fragments.Current);
    }

    [Fact]
    public void DefaultMemberEnumerables_Should_BeEmpty_When_ContainerIsDefault()
    {
        // act
        var variables = default(Utf8VariableDefinitionEnumerable).GetEnumerator();
        var selections = default(Utf8SelectionEnumerable).GetEnumerator();

        // assert
        Assert.False(variables.MoveNext());
        Assert.False(selections.MoveNext());
        Assert.Equal(default, variables.Current);
        Assert.Equal(Utf8SelectionKind.None, selections.Current.Kind);
    }

    [Theory]
    [InlineData(0, Utf8OperationDocumentNode.DbRow.Size)]
    [InlineData(2_147_483_600, 2_147_483_616)]
    public void GetNextMetadataLength_Should_AdvanceOneRow_When_LengthIsWithinLimit(
        int length,
        int expected)
    {
        // act
        var next = Utf8GraphQLOperationParser.GetNextMetadataLength(length);

        // assert
        Assert.Equal(expected, next);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2_147_483_632)]
    [InlineData(int.MaxValue)]
    public void GetNextMetadataLength_Should_Throw_When_NextRowWouldOverflow(int length)
    {
        // act
        var exception = Record.Exception(
            () => Utf8GraphQLOperationParser.GetNextMetadataLength(length));

        // assert
        Assert.IsType<OverflowException>(exception);
    }

    [Fact]
    public void ParseOperationDocument_Should_ResolveAliasAndName_When_CommentsSurroundColon()
    {
        // arrange
        const string source = "{ alias # c\n : # c\n name }";

        // act
        var operation = First(Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes(source)).GetOperations());
        var field = First(operation.SelectionSet.GetSelections()).GetField();

        // assert
        Assert.True(field.HasAlias);
        Assert.Equal("alias", field.Alias);
        Assert.Equal("name", field.Name);
    }

    [Fact]
    public void ParseOperationDocument_Should_ResolveName_When_CommentSeparatesKeywordAndName()
    {
        // arrange
        const string source = "query # c\n Foo { a }";

        // act
        var operation = First(Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes(source)).GetOperations());

        // assert
        Assert.True(operation.HasName);
        Assert.Equal("Foo", operation.Name);
        Assert.Equal(OperationType.Query, operation.Operation);
    }

    [Fact]
    public void ParseOperationDocument_Should_ResolveTypeCondition_When_CommentsSurroundOn()
    {
        // arrange
        const string source = "fragment Frag # c\n on # c\n Item { id }";

        // act
        var fragment = First(Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes(source)).GetFragments());

        // assert
        Assert.Equal("Frag", fragment.Name);
        Assert.Equal("Item", fragment.TypeCondition);
    }

    [Fact]
    public void ParseOperationDocument_Should_NavigatePastVariables_When_FragmentDeclaresVariables()
    {
        // arrange
        const string source = "fragment F($a: Int, $b: String) on Item { id }";

        // act
        var fragment = First(
            Utf8GraphQLOperationParser.Parse(
                Encoding.UTF8.GetBytes(source),
                new ParserOptions(allowFragmentVariables: true)).GetFragments());
        var variableNames = new StringBuilder();
        foreach (var variable in fragment.GetVariableDefinitions())
        {
            variableNames.Append(variable.Name).Append(';');
        }
        var field = First(fragment.SelectionSet.GetSelections()).GetField();

        // assert
        Assert.Equal("a;b;", variableNames.ToString());
        Assert.Equal("Item", fragment.TypeCondition);
        Assert.Equal("id", field.Name);
    }

    [Fact]
    public void GetName_Should_StopAtBufferEnd_When_LastByteIsNameCharacter()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes("a1_Z");
        var document = new Utf8OperationDocumentNode(
            new ReadOnlyMemorySegment(source),
            Utf8OperationDocumentNode.MetaDb.Create([], 0, pooled: false));

        // act
        var whole = document.GetName(0);
        var suffix = document.GetName(2);

        // assert
        Assert.Equal("a1_Z", Encoding.UTF8.GetString(whole));
        Assert.Equal("_Z", Encoding.UTF8.GetString(suffix));
    }

    [Fact]
    public void DbRow_Should_PreserveFields_When_KindOccupiesSignBit()
    {
        // arrange
        var row = new Utf8OperationDocumentNode.DbRow(
            Utf8SyntaxKind.TypeCondition,
            sourceStart: 7,
            sourceEnd: 11,
            nameStart: 7,
            subtreeLength: 1,
            hasName: true);

        // act
        var result = RoundTrip(row);

        // assert
        Assert.Equal(Utf8SyntaxKind.TypeCondition, result.Kind);
        Assert.Equal(7, result.SourceStart);
        Assert.Equal(11, result.SourceEnd);
        Assert.True(result.HasName);
    }

    [Fact]
    public void DbRow_Should_PreserveFlags_When_HasAliasOccupiesSignBit()
    {
        // arrange
        var row = new Utf8OperationDocumentNode.DbRow(
            Utf8SyntaxKind.Field,
            sourceStart: 3,
            sourceEnd: 9,
            nameStart: 5,
            subtreeLength: 1,
            hasAlias: true);

        // act
        var result = RoundTrip(row);

        // assert
        Assert.Equal(Utf8SyntaxKind.Field, result.Kind);
        Assert.True(result.HasAlias);
        Assert.False(result.HasName);
        Assert.Equal(5, result.NameStart);
    }

    [Fact]
    public void DbRow_Should_PreserveValues_When_OffsetsAreMax28Bit()
    {
        // arrange
        const int max = 0x0FFFFFFF;
        var row = new Utf8OperationDocumentNode.DbRow(
            Utf8SyntaxKind.OperationDefinition,
            sourceStart: max,
            sourceEnd: max,
            nameStart: max,
            subtreeLength: max,
            operationType: OperationType.Subscription,
            hasName: true);

        // act
        var result = RoundTrip(row);

        // assert
        Assert.Equal(max, result.SourceStart);
        Assert.Equal(max, result.SourceEnd);
        Assert.Equal(max, result.SubtreeLength);
        Assert.Equal(OperationType.Subscription, result.OperationType);
        Assert.True(result.HasName);
    }

    [Fact]
    public void EnsureSourceWithinLimit_Should_Throw_When_LengthExceedsMaximum()
    {
        // act
        var atLimit = Record.Exception(
            () => Utf8GraphQLOperationParser.EnsureSourceWithinLimit(
                Utf8OperationDocumentNode.DbRow.MaxSourceLength));
        var overLimit = Record.Exception(
            () => Utf8GraphQLOperationParser.EnsureSourceWithinLimit(
                Utf8OperationDocumentNode.DbRow.MaxSourceLength + 1));

        // assert
        Assert.Null(atLimit);
        Assert.IsType<ArgumentException>(overLimit);
    }

    [Fact]
    public void Parse_Should_ExposeSameTree_When_SourceIsOffsetSegment()
    {
        // arrange
        const string source = "query Q { hero: name } fragment F on Person { id }";
        var text = Encoding.UTF8.GetBytes(source);
        var buffer = new byte[8 + text.Length + 5];
        buffer.AsSpan().Fill((byte)'#');
        text.CopyTo(buffer, 8);
        var segment = new ReadOnlyMemorySegment(new ArrayMemoryOwner(buffer), 8, text.Length);

        // act
        var document = Utf8GraphQLOperationParser.Parse(segment);
        var operation = First(document.GetOperations());
        var field = First(operation.SelectionSet.GetSelections()).GetField();
        var fragment = First(document.GetFragments());

        // assert
        Assert.Equal("Q", operation.Name);
        Assert.Equal("hero", field.Alias);
        Assert.Equal("name", field.Name);
        Assert.Equal("Person", fragment.TypeCondition);
    }

    [Fact]
    public void Parse_Should_ReturnEquivalentDocument_When_MetaDbIsPooled()
    {
        // arrange
        const string source = "query Q { hero: name { id } } fragment F on Person { id }";
        var segment = new ReadOnlyMemorySegment(Encoding.UTF8.GetBytes(source));

        // act
        var exact = Utf8GraphQLOperationParser.Parse(segment);
        var pooled = Utf8GraphQLOperationParser.Parse(segment, pooledMetaDb: true);
        var equivalent = Describe(exact) == Describe(pooled);
        pooled.Dispose();

        // assert
        Assert.True(equivalent);
    }

    [Fact]
    public void Dispose_Should_BlockRowAccess_When_MetaDbIsPooled()
    {
        // arrange
        var document = Utf8GraphQLOperationParser.Parse(
            new ReadOnlyMemorySegment(Encoding.UTF8.GetBytes("query Q { field }")),
            pooledMetaDb: true);
        var operation = First(document.GetOperations());
        document.Dispose();

        // act
        var exception = Record.Exception(() => _ = operation.Name);

        // assert
        Assert.IsType<ObjectDisposedException>(exception);
    }

    [Fact]
    public void Parse_Should_AdoptArrayInPlace_When_SourceIsByteArray()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes("query Q { field }");

        // act
        var document = Utf8GraphQLOperationParser.Parse(source);
        var operation = First(document.GetOperations());
        var name = operation.Name;
        source[Array.IndexOf(source, (byte)'Q')] = (byte)'Z';
        var mutatedName = Encoding.UTF8.GetString(operation.Utf8Name);
        var dispose = Record.Exception(document.Dispose);

        // assert
        Assert.Equal("Q", name);
        Assert.Equal("Z", mutatedName);
        Assert.Null(dispose);
    }

    [Fact]
    public void Dispose_Should_BeIdempotent_When_CalledTwice()
    {
        // arrange
        var document = Utf8GraphQLOperationParser.Parse(
            new ReadOnlyMemorySegment(Encoding.UTF8.GetBytes("{ field }")),
            pooledMetaDb: true);

        // act
        document.Dispose();
        var second = Record.Exception(document.Dispose);

        // assert
        Assert.Null(second);
    }

    [Fact]
    public void Parse_Should_Throw_When_SegmentIsEmpty()
    {
        // act
        var exception = Record.Exception(
            () => Utf8GraphQLOperationParser.Parse(default(ReadOnlyMemorySegment)));

        // assert
        Assert.IsType<ArgumentException>(exception);
    }

    private static Utf8OperationDocumentNode.DbRow RoundTrip(Utf8OperationDocumentNode.DbRow row)
    {
        Span<byte> buffer = stackalloc byte[Utf8OperationDocumentNode.DbRow.Size];
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(buffer), row);
        return MemoryMarshal.Read<Utf8OperationDocumentNode.DbRow>(buffer);
    }

    private static Utf8OperationDefinitionNode First(Utf8OperationDefinitionEnumerable enumerable)
    {
        var enumerator = enumerable.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        return enumerator.Current;
    }

    private static Utf8SelectionNode First(Utf8SelectionEnumerable enumerable)
    {
        var enumerator = enumerable.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        return enumerator.Current;
    }

    private static Utf8FragmentDefinitionNode First(Utf8FragmentDefinitionEnumerable enumerable)
    {
        var enumerator = enumerable.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        return enumerator.Current;
    }

    private static string Describe(Utf8OperationDocumentNode document)
    {
        var description = new StringBuilder();
        foreach (var operation in document.GetOperations())
        {
            description
                .Append("operation ")
                .Append(operation.Operation)
                .Append(' ')
                .Append(operation.Name)
                .AppendLine();
            foreach (var variable in operation.GetVariableDefinitions())
            {
                description.Append("  variable ").AppendLine(variable.Name);
            }
            Describe(operation.SelectionSet, description, 1);
        }

        foreach (var fragment in document.GetFragments())
        {
            description
                .Append("fragment ")
                .Append(fragment.Name)
                .Append(" on ")
                .AppendLine(fragment.TypeCondition);
            foreach (var variable in fragment.GetVariableDefinitions())
            {
                description.Append("  variable ").AppendLine(variable.Name);
            }
            Describe(fragment.SelectionSet, description, 1);
        }

        return description.ToString();
    }

    private static void Describe(
        Utf8SelectionSetNode selectionSet,
        StringBuilder description,
        int depth)
    {
        foreach (var selection in selectionSet.GetSelections())
        {
            description.Append(' ', depth * 2);
            switch (selection.Kind)
            {
                case Utf8SelectionKind.Field:
                    var field = selection.GetField();
                    description.Append("field ");
                    if (field.HasAlias)
                    {
                        description.Append(field.Alias).Append(':');
                    }
                    description.AppendLine(field.Name);
                    if (field.HasSelectionSet)
                    {
                        Describe(field.SelectionSet, description, depth + 1);
                    }
                    break;

                case Utf8SelectionKind.FragmentSpread:
                    description
                        .Append("spread ")
                        .AppendLine(selection.GetFragmentSpread().Name);
                    break;

                case Utf8SelectionKind.InlineFragment:
                    var inlineFragment = selection.GetInlineFragment();
                    description.Append("inline");
                    if (inlineFragment.HasTypeCondition)
                    {
                        description.Append(' ').Append(inlineFragment.TypeCondition);
                    }
                    description.AppendLine();
                    Describe(inlineFragment.SelectionSet, description, depth + 1);
                    break;
            }
        }
    }
}
