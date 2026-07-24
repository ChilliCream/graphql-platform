using System.Buffers;
using System.Text;

namespace HotChocolate.Language;

public class Utf8OperationDocumentBuilderTests
{
    [Fact]
    public void Complete_Should_EmitBatchedOperation_When_TwoItemsShareOneDocument()
    {
        // arrange
        var document = Parse(
            "query($id: ID!, $first: Int) { productById(id: $id) { name(first: $first) } }");
        var field = FirstField(document);
        var firstDefinition = VariableDefinition(document, "first");
        var writer = new ArrayBufferWriter<byte>();

        // act
        Utf8OperationDocumentBuilder.New(writer)
            .SetName("Foo")
            .AddSharedVariable(firstDefinition)
            .AddRootSelection(field)
            .AddRootSelection(field)
            .Complete();

        // assert
        Text(writer).MatchInlineSnapshot(
            "query Foo($first: Int, $_0_id: ID!, $_1_id: ID!) { "
            + "_0_productById: productById(id: $_0_id) { name(first: $first) } "
            + "_1_productById: productById(id: $_1_id) { name(first: $first) } }");
    }

    [Fact]
    public void Complete_Should_Roundtrip_When_OutputParsedAgain()
    {
        // arrange
        var document = Parse(
            "query($id: ID!, $first: Int) { productById(id: $id) { name(first: $first) } }");
        var field = FirstField(document);
        var firstDefinition = VariableDefinition(document, "first");
        var writer = new ArrayBufferWriter<byte>();
        Utf8OperationDocumentBuilder.New(writer)
            .SetName("Foo")
            .AddSharedVariable(firstDefinition)
            .AddRootSelection(field)
            .AddRootSelection(field)
            .Complete();

        // act
        var reparsed = Utf8GraphQLOperationParser.Parse(writer.WrittenSpan.ToArray());
        var operation = FirstOperation(reparsed);

        // assert
        Assert.Equal("Foo", Encoding.UTF8.GetString(operation.Utf8Name));
        Assert.Equal(2, CountSelections(operation));
    }

    [Fact]
    public void Complete_Should_EmitAnonymousQueryWithParentheses_When_NameNotSetAndItemHasVariables()
    {
        // arrange
        var document = Parse("query($id: ID!) { a(id: $id) }");
        var field = FirstField(document);
        var writer = new ArrayBufferWriter<byte>();

        // act
        Utf8OperationDocumentBuilder.New(writer)
            .AddRootSelection(field)
            .Complete();

        // assert
        Text(writer).MatchInlineSnapshot("query($_0_id: ID!) { _0_a: a(id: $_0_id) }");
    }

    [Fact]
    public void Complete_Should_OmitParentheses_When_DocumentHasNoVariables()
    {
        // arrange
        var document = Parse("{ productById { name } }");
        var field = FirstField(document);
        var writer = new ArrayBufferWriter<byte>();

        // act
        Utf8OperationDocumentBuilder.New(writer)
            .AddRootSelection(field)
            .Complete();

        // assert
        Text(writer).MatchInlineSnapshot("query { _0_productById: productById { name } }");
    }

    [Fact]
    public void Complete_Should_RenamePerIndex_When_ItemsComeFromDifferentDocuments()
    {
        // arrange
        var documentA = Parse("query($id: ID!) { a(id: $id) }");
        var documentB = Parse("query($id: ID!) { b(id: $id) }");
        var writer = new ArrayBufferWriter<byte>();

        // act
        Utf8OperationDocumentBuilder.New(writer)
            .AddRootSelection(FirstField(documentA))
            .AddRootSelection(FirstField(documentB))
            .Complete();

        // assert
        Text(writer).MatchInlineSnapshot(
            "query($_0_id: ID!, $_1_id: ID!) { _0_a: a(id: $_0_id) _1_b: b(id: $_1_id) }");
    }

    [Fact]
    public void AddRootSelection_Should_PreserveDescription_When_DefinitionIsRenamed()
    {
        // arrange
        var document = Parse("query(\"the id\" $id: ID!) { productById(id: $id) { name } }");
        var field = FirstField(document);
        var writer = new ArrayBufferWriter<byte>();

        // act
        Utf8OperationDocumentBuilder.New(writer)
            .AddRootSelection(field)
            .Complete();

        // assert
        Text(writer).MatchInlineSnapshot(
            "query(\"the id\" $_0_id: ID!) { _0_productById: productById(id: $_0_id) { name } }");
    }

    [Fact]
    public void SetName_Should_Throw_When_CalledAfterAddRootSelection()
    {
        // arrange
        var document = Parse("query { a { b } }");
        var writer = Utf8OperationDocumentBuilder.New(new ArrayBufferWriter<byte>())
            .AddRootSelection(FirstField(document));

        // act
        void Act() => writer.SetName("Foo");

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void AddSharedVariable_Should_Throw_When_CalledAfterAddRootSelection()
    {
        // arrange
        var document = Parse("query($id: ID!) { a(id: $id) }");
        var definition = VariableDefinition(document, "id");
        var writer = Utf8OperationDocumentBuilder.New(new ArrayBufferWriter<byte>())
            .AddRootSelection(FirstField(document));

        // act
        void Act() => writer.AddSharedVariable(definition);

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void AddRootSelection_Should_Throw_When_CalledAfterComplete()
    {
        // arrange
        var document = Parse("query { a { b } }");
        var field = FirstField(document);
        var writer = Utf8OperationDocumentBuilder.New(new ArrayBufferWriter<byte>())
            .AddRootSelection(field);
        writer.Complete();

        // act
        void Act() => writer.AddRootSelection(field);

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void Complete_Should_Throw_When_CalledTwice()
    {
        // arrange
        var document = Parse("query { a { b } }");
        var writer = Utf8OperationDocumentBuilder.New(new ArrayBufferWriter<byte>())
            .AddRootSelection(FirstField(document));
        writer.Complete();

        // act
        void Act() => writer.Complete();

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void Complete_Should_Throw_When_NoRootSelections()
    {
        // arrange
        var writer = Utf8OperationDocumentBuilder.New(new ArrayBufferWriter<byte>())
            .SetName("Foo");

        // act
        void Act() => writer.Complete();

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void AddRootSelection_Should_Throw_When_SelectionHasFragmentSpread()
    {
        // arrange
        var document = Parse(
            "query { productById { ...Frag } } fragment Frag on Product { name }");
        var field = FirstField(document);
        var writer = Utf8OperationDocumentBuilder.New(new ArrayBufferWriter<byte>());

        // act
        void Act() => writer.AddRootSelection(field);

        // assert
        Assert.Throws<NotSupportedException>(Act);
    }

    [Fact]
    public void AddRootSelection_Should_Throw_When_SelectionHasAlias()
    {
        // arrange
        var document = Parse("query($id: ID!) { alias: productById(id: $id) { name } }");
        var field = FirstField(document);
        var writer = Utf8OperationDocumentBuilder.New(new ArrayBufferWriter<byte>());

        // act
        void Act() => writer.AddRootSelection(field);

        // assert
        Assert.Throws<NotSupportedException>(Act);
    }

    [Fact]
    public void Complete_Should_UsePooledOrdinalSet_When_DocumentHasMoreThan64Variables()
    {
        // arrange
        var document = Parse(GenerateManyVariableDocument(70));
        var field = FirstField(document);
        var shared = VariableDefinition(document, "v0");
        var writer = new ArrayBufferWriter<byte>();

        // act
        Utf8OperationDocumentBuilder.New(writer)
            .AddSharedVariable(shared)
            .AddRootSelection(field)
            .Complete();

        // assert
        var text = Text(writer);
        Assert.Contains("$v0: Int", text);
        Assert.Contains("$_0_v65", text);
    }

    private static string GenerateManyVariableDocument(int count)
    {
        var definitions = new StringBuilder();
        var arguments = new StringBuilder();

        for (var i = 0; i < count; i++)
        {
            if (i > 0)
            {
                definitions.Append(", ");
                arguments.Append(", ");
            }

            definitions.Append($"$v{i}: Int");
            arguments.Append($"a{i}: $v{i}");
        }

        return $"query({definitions}) {{ f({arguments}) }}";
    }

    private static Utf8OperationDocument Parse(string source)
        => Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes(source));

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

    private static Utf8VariableDefinitionNode VariableDefinition(
        Utf8OperationDocument document,
        string name)
    {
        foreach (var definition in FirstOperation(document).GetVariableDefinitions())
        {
            if (Encoding.UTF8.GetString(definition.Utf8Name) == name)
            {
                return definition;
            }
        }

        throw new InvalidOperationException($"The variable '{name}' was not found.");
    }

    private static int CountSelections(Utf8OperationDefinitionNode operation)
    {
        var count = 0;
        foreach (var _ in operation.SelectionSet.GetSelections())
        {
            count++;
        }

        return count;
    }
}
