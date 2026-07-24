using System.Buffers;
using System.Text;
using HotChocolate.Buffers;

namespace HotChocolate.Language;

public class Utf8SyntaxFormatterTests
{
    [Fact]
    public void Format_Should_RoundtripSource_When_MapIsEmpty()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes("query Foo($a: Int!) { abc(a: $a) { b } }");
        var document = Utf8GraphQLOperationParser.Parse(source);
        var writer = new ArrayBufferWriter<byte>();

        // act
        document.Format(writer);

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
        document.Format(writer, map);

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
        document.Format(writer, map);

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
        document.Format(writer, map);

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
        field.Format(writer, map);

        // assert
        Text(writer).MatchInlineSnapshot("r: abc(a: $_0_a) { b }");
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
        document.Format(documentDirect, map);
        ((IUtf8SyntaxNode)document).Format(documentBoxed, map);
        var fieldDirect = new ArrayBufferWriter<byte>();
        var fieldBoxed = new ArrayBufferWriter<byte>();
        field.Format(fieldDirect, map);
        ((IUtf8SyntaxNode)field).Format(fieldBoxed, map);

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
        void Act() => document.Format(writer, map);

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

    private static Utf8FieldNode FirstField(Utf8OperationDocument document)
    {
        var selections = FirstOperation(document).SelectionSet.GetSelections().GetEnumerator();
        Assert.True(selections.MoveNext());
        return selections.Current.GetField();
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
