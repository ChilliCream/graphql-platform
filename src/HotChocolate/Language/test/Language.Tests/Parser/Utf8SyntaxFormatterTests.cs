using System.Buffers;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;

namespace HotChocolate.Language;

public class Utf8SyntaxFormatterTests
{
    private const string IndentedSource =
        """
        # leading comment
        query Foo($a: Int!, $b: String) {
          # a comment
          field(arg: $a, other: $b) @skip(if: $a) {
            nested {
              id
            }
          }
        }
        """;

    private const string AdjacentTokenSource =
        """
        query Foo {
          a(list: [1 -2, 3.5, -4]) @x @y {
            ...F
            ... on Thing { id }
          }
        }

        fragment F on Thing {
          id
        }
        """;

    private const string KitchenSinkSource =
        """"
        query Q($a: [ID!]! = ["x"], $b: Int = 1 @onVariable) @onOperation {
          alias: f(x: $a, y: { z: [1, -2, 3.5, null] }, t: """
          text
          """) @onField {
            ...F @onSpread
            ... on Thing @onInline { id }
            ... { id }
          }
        }

        fragment F on Thing @onFragment {
          id(unit: SECONDS)
        }
        """";

    [Fact]
    public void Format_Should_RoundtripSource_When_MapIsEmpty()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes("query Foo($a: Int!) { abc(a: $a) { b } }");
        var document = Utf8GraphQLOperationParser.Parse(source);
        var writer = new ArrayBufferWriter<byte>();

        // act
        document.Format(writer, indented: true);

        // assert
        Assert.Equal(source, writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void Format_Should_SubstituteVariableNames_When_MapProvided()
    {
        // arrange
        var document = Parse("query Foo($a: Int!) { abc(a: $a) { b } }");
        var map = new Utf8VariableNameMap([Bytes("_0_a")]);
        var writer = new ArrayBufferWriter<byte>();

        // act
        document.Format(writer, indented: true, variables: map);

        // assert
        Text(writer).MatchInlineSnapshot("query Foo($_0_a: Int!) { abc(a: $_0_a) { b } }");
    }

    [Fact]
    public void Format_Should_SubstituteNestedSites_When_VariablesInInputObjectsAndDirectives()
    {
        // arrange
        var document = Parse(
            """
            query Q($a: Int!, $b: String!) {
              f(filter: { ids: [$a, 2], name: $b }) @skip(if: $a) {
                # keep me
                id
              }
            }
            """);
        var map = new Utf8VariableNameMap([Bytes("alpha"), Bytes("beta")]);
        var writer = new ArrayBufferWriter<byte>();

        // act
        document.Format(writer, indented: true, variables: map);

        // assert
        Text(writer).MatchInlineSnapshot(
            """
            query Q($alpha: Int!, $beta: String!) {
              f(filter: { ids: [$alpha, 2], name: $beta }) @skip(if: $alpha) {
                # keep me
                id
              }
            }
            """);
    }

    [Fact]
    public void Format_Should_KeepOriginalName_When_MapEntryMissingOrEmpty()
    {
        // arrange
        var document = Parse("query Q($a: Int, $b: Int, $c: Int) { f(x: $a, y: $b, z: $c) }");
        var map = new Utf8VariableNameMap([Bytes("A"), ReadOnlyMemory<byte>.Empty]);
        var writer = new ArrayBufferWriter<byte>();

        // act
        document.Format(writer, indented: true, variables: map);

        // assert
        Text(writer).MatchInlineSnapshot(
            "query Q($A: Int, $b: Int, $c: Int) { f(x: $A, y: $b, z: $c) }");
    }

    [Fact]
    public void Format_Should_EmitSubtreeOnly_When_CalledOnFieldView()
    {
        // arrange
        var document = Parse("query Foo($a: Int!) { r: abc(a: $a) { b } }");
        var field = FirstField(document);
        var map = new Utf8VariableNameMap([Bytes("_0_a")]);
        var writer = new ArrayBufferWriter<byte>();

        // act
        field.Format(writer, indented: true, variables: map);

        // assert
        Text(writer).MatchInlineSnapshot("r: abc(a: $_0_a) { b }");
    }

    [Fact]
    public void SelectionSet_Should_SkipArgumentValues_When_ArgumentsCarryObjectFields()
    {
        // arrange
        // An object value contributes rows to the same sibling run as the selection set, so the
        // lookup has to hop over each argument instead of descending into its value.
        var withoutSelectionSet = FirstField(Parse("{ a(x: { y: 1 }) @d(z: [{ w: 2 }]) }"));
        var withSelectionSet = FirstField(Parse("{ b(x: { y: 1 }) { c } }"));
        var writer = new ArrayBufferWriter<byte>();

        // act
        withSelectionSet.SelectionSet.Format(writer, indented: false);

        // assert
        Assert.False(withoutSelectionSet.HasSelectionSet);
        Text(writer).MatchInlineSnapshot("{c}");
    }

    [Fact]
    public void Format_Should_WorkThroughInterface_When_NodeIsBoxed()
    {
        // arrange
        var document = Parse("query Foo($a: Int!) { abc(a: $a) { b } }");
        var field = FirstField(document);
        var map = new Utf8VariableNameMap([Bytes("_0_a")]);

        // act
        var documentDirect = new ArrayBufferWriter<byte>();
        var documentBoxed = new ArrayBufferWriter<byte>();
        document.Format(documentDirect, indented: true, variables: map);
        ((IUtf8SyntaxNode)document).Format(documentBoxed, indented: true, variables: map);
        var fieldDirect = new ArrayBufferWriter<byte>();
        var fieldBoxed = new ArrayBufferWriter<byte>();
        field.Format(fieldDirect, indented: true, variables: map);
        ((IUtf8SyntaxNode)field).Format(fieldBoxed, indented: true, variables: map);

        // assert
        Assert.Equal(documentDirect.WrittenSpan.ToArray(), documentBoxed.WrittenSpan.ToArray());
        Assert.Equal(fieldDirect.WrittenSpan.ToArray(), fieldBoxed.WrittenSpan.ToArray());
    }

    [Fact]
    public void Format_Should_Throw_When_DocumentIsDisposed()
    {
        // arrange
        var document = Utf8GraphQLOperationParser.Parse(
            new ReadOnlyMemorySegment(Encoding.UTF8.GetBytes("query Foo($a: Int!) { abc(a: $a) { b } }")),
            pooledMetaDb: true);
        var map = new Utf8VariableNameMap([Bytes("_0_a")]);
        var writer = new ArrayBufferWriter<byte>();
        document.Dispose();

        // act
        void Act() => document.Format(writer, indented: true, variables: map);

        // assert
        Assert.Throws<ObjectDisposedException>(Act);
    }

    [Fact]
    public void Ordinal_Should_MatchDeclarationIndex_When_SingleOperation()
    {
        // arrange
        var document = Parse("query Q($a: Int, $b: Int, $c: Int) { f(x: $a, y: $b, z: $c) }");
        var variables = VariableDefinitions(FirstOperation(document));

        // act
        var ordinals = new[] { variables[0].Ordinal, variables[1].Ordinal, variables[2].Ordinal };

        // assert
        Assert.Equal([0, 1, 2], ordinals);
    }

    [Fact]
    public void Ordinal_Should_ReflectFirstOccurrence_When_FragmentPrecedesOperation()
    {
        // arrange
        // $b first occurs in fragment F, so it is assigned ordinal 0 even though the operation
        // declares $a before $b.
        var document = Parse(
            "fragment F on Foo { g(x: $b) } query Q($a: Int, $b: Int) { ...F f(x: $a) }");
        var variables = VariableDefinitions(FirstOperation(document));

        // act
        var ordinals = new[] { variables[0].Ordinal, variables[1].Ordinal };

        // assert
        Assert.Equal([1, 0], ordinals);
    }

    [Fact]
    public void Format_Should_ReproduceSource_When_IndentedIsTrue()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(IndentedSource);
        var document = Utf8GraphQLOperationParser.Parse(source);
        var writer = new ArrayBufferWriter<byte>();

        // act
        document.Format(writer, indented: true);

        // assert
        Assert.Equal(source, writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void Format_Should_CollapseWhitespaceAndComments_When_IndentedIsFalse()
    {
        // act
        var compact = FormatCompactAndReparse(Parse(IndentedSource));

        // assert
        compact.MatchInlineSnapshot(
            "query Foo($a:Int!,$b:String){field(arg:$a,other:$b)@skip(if:$a){nested{id}}}");
    }

    [Fact]
    public void Format_Should_KeepDocumentStructure_When_CompactOutputIsReparsed()
    {
        // arrange
        var document = Parse(IndentedSource);
        var writer = new ArrayBufferWriter<byte>();
        document.Format(writer, indented: false);

        // act
        var reparsed = Utf8GraphQLOperationParser.Parse(writer.WrittenSpan.ToArray());
        var operation = FirstOperation(reparsed);

        // assert
        Assert.Equal("Foo", Encoding.UTF8.GetString(operation.Utf8Name));
        Assert.Equal(2, VariableDefinitions(operation).Count);
        Assert.Equal("field", Encoding.UTF8.GetString(FirstField(reparsed).Utf8Name));
    }

    [Fact]
    public void Format_Should_PreserveStringLiterals_When_IndentedIsFalse()
    {
        // arrange
        var document = Parse(
            """"
            query Q {
              f(
                a: "hello  world"
                b: "he said \"hi\""
                c: """
                  block
                    text
                  """
              )
            }
            """");

        // act
        var compact = FormatCompactAndReparse(document);

        // assert
        compact.MatchInlineSnapshot(
            """"
            query Q{f(a:"hello  world",b:"he said \"hi\"",c:"""
                  block
                    text
                  """)}
            """");
    }

    [Fact]
    public void Format_Should_SeparateAdjacentTokens_When_IndentedIsFalse()
    {
        // act
        var compact = FormatCompactAndReparse(Parse(AdjacentTokenSource));

        // assert
        compact.MatchInlineSnapshot(
            "query Foo{a(list:[1,-2,3.5,-4])@x@y{...F ...on Thing{id}}} "
            + "fragment F on Thing{id}");
    }

    [Fact]
    public void Format_Should_KeepDefinitionNames_When_AdjacentTokenOutputIsReparsed()
    {
        // arrange
        var document = Parse(AdjacentTokenSource);
        var writer = new ArrayBufferWriter<byte>();
        document.Format(writer, indented: false);

        // act
        var reparsed = Utf8GraphQLOperationParser.Parse(writer.WrittenSpan.ToArray());

        // assert
        Assert.Equal("Foo", Encoding.UTF8.GetString(FirstOperation(reparsed).Utf8Name));
        Assert.Equal("F", Encoding.UTF8.GetString(FirstFragment(reparsed).Utf8Name));
    }

    [Fact]
    public void Format_Should_EmitEveryValueKind_When_IndentedIsFalse()
    {
        // arrange
        var document = Parse(
            """
            query Q($v: Int) {
              f(
                a: $v
                b: 1
                c: -1.5e3
                d: "he said \"hi\""
                e: ENUM
                g: true
                h: false
                i: null
                j: [1, [2], { m: $v }]
                k: { n: { o: [] } }
              )
            }
            """);

        // act
        var compact = FormatCompactAndReparse(document);

        // assert
        compact.MatchInlineSnapshot(
            """
            query Q($v:Int){f(a:$v,b:1,c:-1.5e3,d:"he said \"hi\"",e:ENUM,g:true,h:false,i:null,j:[1,[2],{m:$v}],k:{n:{o:[]}})}
            """);
    }

    [Fact]
    public void Format_Should_EmitDirectives_When_EveryConstructIsDecorated()
    {
        // arrange
        var document = Parse(
            """
            query Q($a: Int @onVariable) @onOperation {
              f(x: $a) @onField {
                ...F @onSpread
                ... on Thing @onInline { id }
              }
            }

            fragment F on Thing @onFragment { id }
            """);

        // act
        var compact = FormatCompactAndReparse(document);

        // assert
        compact.MatchInlineSnapshot(
            "query Q($a:Int@onVariable)@onOperation{f(x:$a)@onField{...F@onSpread "
            + "...on Thing@onInline{id}}} fragment F on Thing@onFragment{id}");
    }

    [Fact]
    public void Format_Should_EmitTypesAndDefaults_When_VariablesUseListAndNonNullTypes()
    {
        // arrange
        var document = Parse(
            """
            query Q($a: [ID!]! = ["x"], $b: Int = 3, $c: [[Boolean]] = null, $d: String) {
              f
            }
            """);

        // act
        var compact = FormatCompactAndReparse(document);

        // assert
        compact.MatchInlineSnapshot(
            """
            query Q($a:[ID!]!=["x"],$b:Int=3,$c:[[Boolean]]=null,$d:String){f}
            """);
    }

    [Fact]
    public void Format_Should_EmitTypeCondition_When_FragmentDeclaresVariables()
    {
        // arrange
        var document = Utf8GraphQLOperationParser.Parse(
            Encoding.UTF8.GetBytes("fragment F($a: Int = 1) on Thing { id(x: $a) }"),
            new ParserOptions(allowFragmentVariables: true));

        // act
        var writer = new ArrayBufferWriter<byte>();
        document.Format(writer, indented: false);
        var reparsed = Utf8GraphQLOperationParser.Parse(
            writer.WrittenSpan.ToArray(),
            new ParserOptions(allowFragmentVariables: true));

        // assert
        Assert.Equal("Thing", Encoding.UTF8.GetString(FirstFragment(reparsed).Utf8TypeCondition));
        Text(writer).MatchInlineSnapshot("fragment F($a:Int=1)on Thing{id(x:$a)}");
    }

    [Fact]
    public void Format_Should_EmitVariableDefinitions_When_FragmentArgumentsAllowed()
    {
        // arrange
        var document = Utf8GraphQLOperationParser.Parse(
            Encoding.UTF8.GetBytes("fragment F($a: Int = 1) on Thing { id(x: $a) }"),
            new ParserOptions(new ParserOptionsExperimental(allowFragmentArguments: true)));

        // act
        var writer = new ArrayBufferWriter<byte>();
        document.Format(writer, indented: false);

        // assert
        Text(writer).MatchInlineSnapshot("fragment F($a:Int=1)on Thing{id(x:$a)}");
    }

    [Fact]
    public void Format_Should_EmitKeyword_When_OperationIsShorthandOrMutation()
    {
        // arrange
        var shorthand = Parse("{ a }");
        var mutation = Parse("mutation { a } subscription { b }");

        // act
        var shorthandCompact = FormatCompactAndReparse(shorthand);
        var mutationCompact = FormatCompactAndReparse(mutation);

        // assert
        Assert.Equal("query{a}", shorthandCompact);
        Assert.Equal("mutation{a} subscription{b}", mutationCompact);
    }

    [Fact]
    public void Format_Should_SubstituteVariableNames_When_IndentedIsFalseAndSourceIsMultiLine()
    {
        // arrange
        var document = Parse(
            """
            query Q($a: Int!, $b: String) {
              f(x: $a) {
                g(y: $b, z: $a)
              }
            }
            """);
        var map = new Utf8VariableNameMap([Bytes("alpha"), Bytes("beta")]);

        // act
        var compact = FormatCompactAndReparse(document, map);

        // assert
        compact.MatchInlineSnapshot(
            "query Q($alpha:Int!,$beta:String){f(x:$alpha){g(y:$beta,z:$alpha)}}");
    }

    [Fact]
    public void Format_Should_SubstituteNestedSites_When_IndentedIsFalse()
    {
        // arrange
        var document = Parse(
            """
            query Q($a: Int, $b: Int = 2) {
              f(x: { y: [$a, { z: $b }] }) @d(if: $a)
            }
            """);
        var map = new Utf8VariableNameMap([Bytes("alpha"), Bytes("beta")]);

        // act
        var compact = FormatCompactAndReparse(document, map);

        // assert
        compact.MatchInlineSnapshot(
            "query Q($alpha:Int,$beta:Int=2){f(x:{y:[$alpha,{z:$beta}]})@d(if:$alpha)}");
    }

    [Fact]
    public void Format_Should_PrefixUnsharedVariableNames_When_SourceIsMultiLine()
    {
        // arrange
        var document = Parse(
            """
            query Q($id: ID!, $first: Int) {
              productById(id: $id) {
                name(first: $first)
              }
            }
            """);
        var field = FirstField(document);
        Span<ulong> words = stackalloc ulong[1];
        var shared = new SharedOrdinalSet(words);
        shared.Add(Ordinal(document, "first"));
        var writer = new ArrayBufferWriter<byte>();

        // act
        field.Format(new Utf8SyntaxWriter(writer, escape: false), "_0_"u8, shared);

        // assert
        ReparseSelection(Text(writer)).MatchInlineSnapshot(
            "productById(id:$_0_id){name(first:$first)}");
    }

    [Fact]
    public void Format_Should_SubstituteVariableNames_When_CommentsSeparateEveryToken()
    {
        // arrange
        var document = Parse(
            """
            query # c1
            Foo # c2
            ( # c3
            $ # c4
            a # c5
            : # c6
            Int # c7
            ) # c8
            { # c9
            alias # c10
            : # c11
            field # c12
            ( # c13
            arg # c14
            : # c15
            $ # c16
            a # c17
            ) # c18
            } # c19
            """);
        var map = new Utf8VariableNameMap([Bytes("alpha")]);

        // act
        var compact = FormatCompactAndReparse(document, map);

        // assert
        compact.MatchInlineSnapshot("query Foo($alpha:Int){alias:field(arg:$alpha)}");
    }

    [Fact]
    public void Format_Should_EmitJsonStringLiteral_When_FormatAsJsonStringValue()
    {
        // arrange
        var document = Parse(
            """
            query Q {
              f(a: "he said \"hi\"", b: "c:\\tmp", c: "größe")
            }
            """);
        var writer = new ArrayBufferWriter<byte>();

        // act
        document.Format(writer, indented: false, formatAsJsonStringValue: true);

        // assert
        Text(writer).MatchInlineSnapshot(
            """
            "query Q{f(a:\"he said \\\"hi\\\"\",b:\"c:\\\\tmp\",c:\"größe\")}"
            """);
    }

    [Fact]
    public void Format_Should_WrapOutputInQuotesOnce_When_NodeIsMinimal()
    {
        // arrange
        var document = Parse("{ a }");
        var field = FirstField(document);
        var writer = new ArrayBufferWriter<byte>();

        // act
        field.Format(writer, indented: false, formatAsJsonStringValue: true);

        // assert
        Text(writer).MatchInlineSnapshot("""
            "a"
            """);
    }

    [Fact]
    public void Format_Should_EscapeControlCharacters_When_LiteralsCarryTabAndNewline()
    {
        // arrange
        var document = Parse(
            "query Q { f(a: \"x\ty\", b: \"\"\"\nline1\nline2\"\"\") }");
        var writer = new ArrayBufferWriter<byte>();

        // act
        document.Format(writer, indented: false, formatAsJsonStringValue: true);

        // assert
        Text(writer).MatchInlineSnapshot(
            """
            "query Q{f(a:\"x\ty\",b:\"\"\"\nline1\nline2\"\"\")}"
            """);
    }

    [Fact]
    public void Format_Should_DecodeToPlainOutput_When_CompactJsonStringValueIsParsed()
    {
        // arrange
        var document = Parse(
            """
            query Q($a: Int!) {
              f(a: $a, b: "he said \"hi\"", c: "c:\\tmp") { id }
            }
            """);

        // act
        var decoded = FormatAndDecode(document, indented: false, variables: default);

        // assert
        Assert.Equal(FormatAsText(document, indented: false, variables: default), decoded);
    }

    [Fact]
    public void Format_Should_DecodeToPlainOutput_When_VerbatimJsonStringValueIsParsed()
    {
        // arrange
        var document = Parse(
            """
            query Q($a: Int!) {
              # a comment
              f(a: $a, b: "he said \"hi\"") { id }
            }
            """);

        // act
        var decoded = FormatAndDecode(document, indented: true, variables: default);

        // assert
        Assert.Equal(FormatAsText(document, indented: true, variables: default), decoded);
    }

    [Fact]
    public void Format_Should_DecodeToPlainOutput_When_SubstitutedJsonStringValueIsParsed()
    {
        // arrange
        var document = Parse(
            """
            query Q($a: Int!) {
              f(a: $a, b: "he said \"hi\"") { id }
            }
            """);
        var map = new Utf8VariableNameMap([Bytes("_0_a")]);

        // act
        var decoded = FormatAndDecode(document, indented: false, variables: map);

        // assert
        Assert.Equal(FormatAsText(document, indented: false, variables: map), decoded);
    }

    [Fact]
    public void Format_Should_DecodeToPlainOutput_When_DocumentUsesEveryConstruct()
    {
        // arrange
        var document = Parse(KitchenSinkSource);

        // act
        var decoded = FormatAndDecode(document, indented: false, variables: default);
        var reparsed = Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes(decoded));

        // assert
        Assert.Equal(FormatAsText(document, indented: false, variables: default), decoded);
        Assert.Equal("Q", Encoding.UTF8.GetString(FirstOperation(reparsed).Utf8Name));
        Assert.Equal("F", Encoding.UTF8.GetString(FirstFragment(reparsed).Utf8Name));
    }

    [Fact]
    public void Format_Should_EmitEveryConstruct_When_IndentedIsFalse()
    {
        // act
        var compact = FormatCompactAndReparse(Parse(KitchenSinkSource));

        // assert
        compact.MatchInlineSnapshot(
            """"
            query Q($a:[ID!]!=["x"],$b:Int=1@onVariable)@onOperation{alias:f(x:$a,y:{z:[1,-2,3.5,null]},t:"""
              text
              """)@onField{...F@onSpread ...on Thing@onInline{id} ...{id}}} fragment F on Thing@onFragment{id(unit:SECONDS)}
            """");
    }

    /// <summary>
    /// Parses the emitted selection inside a minimal operation so that a canonical snapshot of a
    /// selection is anchored to a valid GraphQL document, and returns the emitted text.
    /// </summary>
    private static string ReparseSelection(string selection)
    {
        Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes($"{{{selection}}}"));
        return selection;
    }

    /// <summary>
    /// Formats the document in canonical form, parses the output again so that every canonical
    /// snapshot is anchored to a valid GraphQL document, and returns the emitted text.
    /// </summary>
    private static string FormatCompactAndReparse(
        Utf8OperationDocument document,
        Utf8VariableNameMap variables = default)
    {
        var writer = new ArrayBufferWriter<byte>();
        document.Format(writer, indented: false, variables: variables);
        var text = writer.WrittenSpan.ToArray();
        Utf8GraphQLOperationParser.Parse(text);
        return Encoding.UTF8.GetString(text);
    }

    private static string FormatAsText(
        Utf8OperationDocument document,
        bool indented,
        Utf8VariableNameMap variables)
    {
        var writer = new ArrayBufferWriter<byte>();
        document.Format(writer, indented, variables: variables);
        return Text(writer);
    }

    private static string FormatAndDecode(
        Utf8OperationDocument document,
        bool indented,
        Utf8VariableNameMap variables)
    {
        var writer = new ArrayBufferWriter<byte>();
        document.Format(writer, indented, formatAsJsonStringValue: true, variables);
        using var json = JsonDocument.Parse(writer.WrittenMemory);
        return json.RootElement.GetString()!;
    }

    private static Utf8OperationDocument Parse(string source)
        => Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes(source));

    private static ReadOnlyMemory<byte> Bytes(string value)
        => Encoding.UTF8.GetBytes(value);

    private static string Text(ArrayBufferWriter<byte> writer)
        => Encoding.UTF8.GetString(writer.WrittenSpan);

    private static Utf8OperationDefinitionNode FirstOperation(Utf8OperationDocument document)
    {
        var operations = document.GetOperations().GetEnumerator();
        Assert.True(operations.MoveNext());
        return operations.Current;
    }

    private static Utf8FragmentDefinitionNode FirstFragment(Utf8OperationDocument document)
    {
        var fragments = document.GetFragments().GetEnumerator();
        Assert.True(fragments.MoveNext());
        return fragments.Current;
    }

    private static Utf8FieldNode FirstField(Utf8OperationDocument document)
    {
        var selections = FirstOperation(document).SelectionSet.GetSelections().GetEnumerator();
        Assert.True(selections.MoveNext());
        return selections.Current.GetField();
    }

    private static int Ordinal(Utf8OperationDocument document, string name)
    {
        foreach (var definition in FirstOperation(document).GetVariableDefinitions())
        {
            if (Encoding.UTF8.GetString(definition.Utf8Name) == name)
            {
                return definition.Ordinal;
            }
        }

        throw new InvalidOperationException($"The variable '{name}' was not found.");
    }

    private static List<Utf8VariableDefinitionNode> VariableDefinitions(
        Utf8OperationDefinitionNode operation)
    {
        var list = new List<Utf8VariableDefinitionNode>();
        foreach (var variable in operation.GetVariableDefinitions())
        {
            list.Add(variable);
        }

        return list;
    }
}
