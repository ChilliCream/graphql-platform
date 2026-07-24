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
        var arrayFields = typeof(Utf8OperationDocument)
            .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Where(t => t.FieldType.IsArray)
            .Select(t => t.FieldType)
            .ToArray();

        // assert
        Assert.Equal(Encoding.UTF8.GetByteCount(source), document.SourceLength);
        Assert.Equal(15 * Utf8OperationDocument.DbRow.Size, document.MetadataLength);
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
        // rows: [OperationQuery][Name Q][VariableDefinition][Name value]...
        var document = Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes(source));
        var variable = document.GetRow(2);

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
    [InlineData(0, Utf8OperationDocument.DbRow.Size)]
    [InlineData(2_147_483_600, 2_147_483_612)]
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
    [InlineData(2_147_483_637)]
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
    public void DbRow_Should_PreserveValues_When_KindSetsSignBit()
    {
        // arrange
        // FragmentSpread (8) sets the top bit of the kind nybble, locking the unsigned-shift read.
        var row = new Utf8OperationDocument.DbRow(
            Utf8SyntaxKind.FragmentSpread,
            location: 7,
            sizeOrLength: 4,
            numberOfRows: 2);

        // act
        var result = RoundTrip(row);

        // assert
        Assert.Equal(Utf8SyntaxKind.FragmentSpread, result.Kind);
        Assert.Equal(7, result.Location);
        Assert.Equal(11, result.SourceEnd);
        Assert.Equal(2, result.NumberOfRows);
    }

    [Fact]
    public void DbRow_Should_PreserveValues_When_KindIsNameLeaf()
    {
        // arrange
        // Name (11) also sets the top bit of the kind nybble.
        var row = new Utf8OperationDocument.DbRow(
            Utf8SyntaxKind.Name,
            location: 3,
            sizeOrLength: 5,
            numberOfRows: 1);

        // act
        var result = RoundTrip(row);

        // assert
        Assert.Equal(Utf8SyntaxKind.Name, result.Kind);
        Assert.Equal(3, result.Location);
        Assert.Equal(5, result.SizeOrLength);
        Assert.Equal(1, result.NumberOfRows);
    }

    [Fact]
    public void DbRow_Should_PreserveValues_When_LocationAndLengthAreMaxInt()
    {
        // arrange
        var row = new Utf8OperationDocument.DbRow(
            Utf8SyntaxKind.SelectionSet,
            location: int.MaxValue,
            sizeOrLength: int.MaxValue,
            numberOfRows: 0x0FFFFFFF);

        // act
        var result = RoundTrip(row);

        // assert
        Assert.Equal(int.MaxValue, result.Location);
        Assert.Equal(int.MaxValue, result.SizeOrLength);
        Assert.Equal(0x0FFFFFFF, result.NumberOfRows);
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

    [Fact]
    public void Parse_Should_RecordVariableSites_When_VariablesAppearInArgumentsAndDirectives()
    {
        // arrange
        const string source =
            "query Foo($a: Int! = 1 @x(y: 2)) "
            + "{ field(a: $a, w: { inner: [$a, $b] }) @dir(if: $b) { child(c: $a) } }";
        var sourceBytes = Encoding.UTF8.GetBytes(source);

        // act
        var document = Utf8GraphQLOperationParser.Parse(sourceBytes);
        var sites = DescribeSites(document, sourceBytes);

        // assert
        Assert.Equal(2, document.VariableCount);
        sites.MatchInlineSnapshot(
            """
            0 @11 'a'
            0 @45 'a'
            0 @62 'a'
            1 @66 'b'
            1 @82 'b'
            0 @97 'a'
            """);
    }

    [Fact]
    public void Parse_Should_ShareOrdinal_When_TwoOperationsUseSameVariableName()
    {
        // arrange
        const string source = "query A($a: Int) { f(x: $a) } query B($a: Int) { g(y: $a) }";
        var document = Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes(source));

        // act
        var ordinals = new int[document.VariableSiteCount];
        for (var i = 0; i < ordinals.Length; i++)
        {
            ordinals[i] = document.GetVariableSiteOrdinal(i);
        }

        // assert
        Assert.Equal(1, document.VariableCount);
        Assert.Equal([0, 0, 0, 0], ordinals);
    }

    [Fact]
    public void Parse_Should_ReturnEmptyTable_When_DocumentHasNoVariables()
    {
        // act
        var document = Utf8GraphQLOperationParser.Parse(
            Encoding.UTF8.GetBytes("query Q { field { child } }"));

        // assert
        Assert.Equal(0, document.VariableSiteCount);
        Assert.Equal(0, document.VariableCount);
    }

    [Fact]
    public void Parse_Should_RecordDefinitionSite_When_TriviaSeparatesDollarAndName()
    {
        // arrange
        const string source = "query ($ #c\n a: Int) { f(x: $a) }";
        var sourceBytes = Encoding.UTF8.GetBytes(source);

        // act
        var document = Utf8GraphQLOperationParser.Parse(sourceBytes);
        var definitionSite = document.GetVariableSitePosition(0);

        // assert
        Assert.Equal((byte)'a', sourceBytes[definitionSite]);
        Assert.Equal(source.IndexOf("a: Int", StringComparison.Ordinal), definitionSite);
    }

    [Fact]
    public void Parse_Should_RecordFragmentVariableSites_When_FragmentVariablesAllowed()
    {
        // arrange
        const string source = "fragment F($a: Int) on Item { f(x: $a) }";
        var sourceBytes = Encoding.UTF8.GetBytes(source);

        // act
        var document = Utf8GraphQLOperationParser.Parse(
            sourceBytes,
            new ParserOptions(allowFragmentVariables: true));

        // assert
        Assert.Equal(1, document.VariableCount);
        Assert.Equal("a", Encoding.UTF8.GetString(document.GetVariableName(0)));
        Assert.Equal(2, document.VariableSiteCount);
    }

    [Fact]
    public void Dispose_Should_BlockVariableTableAccess_When_MetaDbIsPooled()
    {
        // arrange
        var document = Utf8GraphQLOperationParser.Parse(
            new ReadOnlyMemorySegment(Encoding.UTF8.GetBytes("query Q($a: Int) { f(x: $a) }")),
            pooledMetaDb: true);
        document.Dispose();

        // act
        var exception = Record.Exception(() => document.GetVariableSitePosition(0));

        // assert
        Assert.IsType<ObjectDisposedException>(exception);
    }

    [Fact]
    public void ParseOperationDocument_Should_EmitAliasThenName_When_FieldIsAliased()
    {
        // act
        var document = Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes("{ hero: name }"));

        // assert
        DescribeRows(document).MatchInlineSnapshot(
            """
            OperationQuery (5)
            SelectionSet (4)
            Field (3)
            Alias 'hero'
            Name 'name'
            """);
    }

    [Fact]
    public void ParseOperationDocument_Should_OrderRows_When_FragmentDeclaresVariables()
    {
        // arrange
        const string source = "fragment F($a: Int, $b: String) on Item { id }";

        // act
        var document = Utf8GraphQLOperationParser.Parse(
            Encoding.UTF8.GetBytes(source),
            new ParserOptions(allowFragmentVariables: true));

        // assert
        DescribeRows(document).MatchInlineSnapshot(
            """
            FragmentDefinition (10)
            Name 'F'
            VariableDefinition (2)
            Name 'a'
            VariableDefinition (2)
            Name 'b'
            TypeCondition 'Item'
            SelectionSet (3)
            Field (2)
            Name 'id'
            """);
    }

    [Fact]
    public void ParseOperationDocument_Should_DiscoverNamePresence_When_NamedVsShorthand()
    {
        // act
        var named = First(
            Utf8GraphQLOperationParser.Parse(
                Encoding.UTF8.GetBytes("query Q { field }")).GetOperations());
        var shorthand = First(
            Utf8GraphQLOperationParser.Parse(
                Encoding.UTF8.GetBytes("{ field }")).GetOperations());

        // assert
        Assert.True(named.HasName);
        Assert.Equal("Q", Encoding.UTF8.GetString(named.Utf8Name));
        Assert.False(shorthand.HasName);
        Assert.True(shorthand.Utf8Name.IsEmpty);
    }

    [Fact]
    public void Parse_Should_ReturnVariableNames_When_DocumentHasMultipleVariables()
    {
        // arrange
        const string source = "query Q($alpha: Int, $beta: String) { f(a: $alpha, b: $beta) }";

        // act
        var document = Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes(source));

        // assert
        Assert.Equal(2, document.VariableCount);
        Assert.Equal("alpha", Encoding.UTF8.GetString(document.GetVariableName(0)));
        Assert.Equal("beta", Encoding.UTF8.GetString(document.GetVariableName(1)));
    }

    private static string DescribeRows(Utf8OperationDocument document)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < document.RowCount; i++)
        {
            var row = document.GetRow(i);
            if (i > 0)
            {
                builder.AppendLine();
            }

            builder.Append(row.Kind);
            if (row.Kind is Utf8SyntaxKind.Name
                or Utf8SyntaxKind.Alias
                or Utf8SyntaxKind.TypeCondition)
            {
                builder
                    .Append(" '")
                    .Append(document.GetString(row.Location, row.SizeOrLength))
                    .Append('\'');
            }
            else
            {
                builder.Append(" (").Append(row.NumberOfRows).Append(')');
            }
        }

        return builder.ToString();
    }

    private static string DescribeSites(Utf8OperationDocument document, byte[] source)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < document.VariableSiteCount; i++)
        {
            var position = document.GetVariableSitePosition(i);
            if (i > 0)
            {
                builder.AppendLine();
            }
            builder
                .Append(document.GetVariableSiteOrdinal(i))
                .Append(" @")
                .Append(position)
                .Append(" '")
                .Append((char)source[position])
                .Append('\'');
        }

        return builder.ToString();
    }

    private static Utf8OperationDocument.DbRow RoundTrip(Utf8OperationDocument.DbRow row)
    {
        Span<byte> buffer = stackalloc byte[Utf8OperationDocument.DbRow.Size];
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(buffer), row);
        return MemoryMarshal.Read<Utf8OperationDocument.DbRow>(buffer);
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

    private static string Describe(Utf8OperationDocument document)
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
