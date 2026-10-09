using System.Buffers;
using System.Text;
using System.Text.Json;

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
            .AddRootSelection(field, "Product")
            .AddRootSelection(field, "Product")
            .Complete();

        // assert
        CanonicalText(writer).MatchInlineSnapshot(
            "query Foo($first:Int,$_0_id:ID!,$_1_id:ID!){"
            + "_0_productById:productById(id:$_0_id){..._fusion_body_1} "
            + "_1_productById:productById(id:$_1_id){..._fusion_body_1}} "
            + "fragment _fusion_body_1 on Product{name(first:$first)}");
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
            .AddRootSelection(field, "Product")
            .AddRootSelection(field, "Product")
            .Complete();

        // act
        var reparsed = Reparse(writer);
        var operation = FirstOperation(reparsed);

        // assert
        Assert.Equal("Foo", Encoding.UTF8.GetString(operation.Utf8Name));
        Assert.Equal(2, CountSelections(operation));
    }

    [Fact]
    public void Complete_Should_EmitOneFragment_When_ThreeItemsShareOneBody()
    {
        // arrange
        var document = Parse(
            "query($id: ID!, $first: Int) { productById(id: $id) { name(first: $first) } }");
        var field = FirstField(document);
        var firstDefinition = VariableDefinition(document, "first");
        var writer = new ArrayBufferWriter<byte>();

        // act
        Utf8OperationDocumentBuilder.New(writer)
            .SetName("Batch")
            .AddSharedVariable(firstDefinition)
            .AddRootSelection(field, "Product")
            .AddRootSelection(field, "Product")
            .AddRootSelection(field, "Product")
            .Complete();

        // assert
        CanonicalText(writer).MatchInlineSnapshot(
            "query Batch($first:Int,$_0_id:ID!,$_1_id:ID!,$_2_id:ID!){"
            + "_0_productById:productById(id:$_0_id){..._fusion_body_1} "
            + "_1_productById:productById(id:$_1_id){..._fusion_body_1} "
            + "_2_productById:productById(id:$_2_id){..._fusion_body_1}} "
            + "fragment _fusion_body_1 on Product{name(first:$first)}");
    }

    [Fact]
    public void Complete_Should_ExposeSharedFragment_When_OutputParsedAgain()
    {
        // arrange
        var document = Parse(
            "query($id: ID!, $first: Int) { productById(id: $id) { name(first: $first) } }");
        var field = FirstField(document);
        var firstDefinition = VariableDefinition(document, "first");
        var writer = new ArrayBufferWriter<byte>();
        Utf8OperationDocumentBuilder.New(writer)
            .SetName("Batch")
            .AddSharedVariable(firstDefinition)
            .AddRootSelection(field, "Product")
            .AddRootSelection(field, "Product")
            .AddRootSelection(field, "Product")
            .Complete();

        // act
        var reparsed = Reparse(writer);
        var fragment = new ArrayBufferWriter<byte>();
        SingleFragment(reparsed).Format(fragment);

        // assert
        Assert.Equal(3, CountSelections(FirstOperation(reparsed)));
        Text(fragment).MatchInlineSnapshot(
            "fragment _fusion_body_1 on Product{name(first:$first)}");
    }

    [Fact]
    public void Complete_Should_EmitInlineBodies_When_BodyUsesPerItemVariable()
    {
        // arrange
        // The body reads $first, which is renamed per item, so the items cannot share it.
        var document = Parse(
            "query($id: ID!, $first: Int) { productById(id: $id) { name(first: $first) } }");
        var field = FirstField(document);
        var writer = new ArrayBufferWriter<byte>();

        // act
        Utf8OperationDocumentBuilder.New(writer)
            .AddRootSelection(field, "Product")
            .AddRootSelection(field, "Product")
            .Complete();

        // assert
        CanonicalText(writer).MatchInlineSnapshot(
            "query($_0_id:ID!,$_0_first:Int,$_1_id:ID!,$_1_first:Int){"
            + "_0_productById:productById(id:$_0_id){name(first:$_0_first)} "
            + "_1_productById:productById(id:$_1_id){name(first:$_1_first)}}");
    }

    [Fact]
    public void Complete_Should_EmitInlineBody_When_OnlyOneItemSelectsIt()
    {
        // arrange
        var document = Parse("query($id: ID!) { productById(id: $id) { name } }");
        var field = FirstField(document);
        var writer = new ArrayBufferWriter<byte>();

        // act
        Utf8OperationDocumentBuilder.New(writer)
            .AddRootSelection(field, "Product")
            .Complete();

        // assert
        CanonicalText(writer).MatchInlineSnapshot(
            "query($_0_id:ID!){_0_productById:productById(id:$_0_id){name}}");
    }

    [Fact]
    public void Complete_Should_NumberFragments_When_BatchMixesSharedAndSingleBodies()
    {
        // arrange
        var userDocument = Parse("query($id: ID!) { userById(id: $id) { email } }");
        var productDocument = Parse("query($id: ID!) { productById(id: $id) { name } }");
        var productField = FirstField(productDocument);
        var writer = new ArrayBufferWriter<byte>();

        // act
        Utf8OperationDocumentBuilder.New(writer)
            .AddRootSelection(FirstField(userDocument), "User")
            .AddRootSelection(productField, "Product")
            .AddRootSelection(productField, "Product")
            .Complete();

        // assert
        CanonicalText(writer).MatchInlineSnapshot(
            "query($_0_id:ID!,$_1_id:ID!,$_2_id:ID!){"
            + "_0_userById:userById(id:$_0_id){email} "
            + "_1_productById:productById(id:$_1_id){..._fusion_body_1} "
            + "_2_productById:productById(id:$_2_id){..._fusion_body_1}} "
            + "fragment _fusion_body_1 on Product{name}");
    }

    [Fact]
    public void Complete_Should_NumberFragmentsInRegistrationOrder_When_SharedBodiesInterleave()
    {
        // arrange
        var userDocument = Parse("query($id: ID!) { userById(id: $id) { email } }");
        var productDocument = Parse("query($id: ID!) { productById(id: $id) { name } }");
        var userField = FirstField(userDocument);
        var productField = FirstField(productDocument);
        var writer = new ArrayBufferWriter<byte>();

        // act
        Utf8OperationDocumentBuilder.New(writer)
            .AddRootSelection(userField, "User")
            .AddRootSelection(productField, "Product")
            .AddRootSelection(userField, "User")
            .AddRootSelection(productField, "Product")
            .Complete();

        // assert
        CanonicalText(writer).MatchInlineSnapshot(
            "query($_0_id:ID!,$_1_id:ID!,$_2_id:ID!,$_3_id:ID!){"
            + "_0_userById:userById(id:$_0_id){..._fusion_body_1} "
            + "_1_productById:productById(id:$_1_id){..._fusion_body_2} "
            + "_2_userById:userById(id:$_2_id){..._fusion_body_1} "
            + "_3_productById:productById(id:$_3_id){..._fusion_body_2}} "
            + "fragment _fusion_body_1 on User{email} "
            + "fragment _fusion_body_2 on Product{name}");
    }

    [Fact]
    public void Complete_Should_EmitNoFragment_When_ItemsHaveNoSelectionSet()
    {
        // arrange
        var document = Parse("query($id: ID!) { a(id: $id) }");
        var field = FirstField(document);
        var writer = new ArrayBufferWriter<byte>();

        // act
        Utf8OperationDocumentBuilder.New(writer)
            .AddRootSelection(field, "Query")
            .AddRootSelection(field, "Query")
            .Complete();

        // assert
        CanonicalText(writer).MatchInlineSnapshot(
            "query($_0_id:ID!,$_1_id:ID!){_0_a:a(id:$_0_id) _1_a:a(id:$_1_id)}");
    }

    [Fact]
    public void Complete_Should_EmitCompactOperation_When_SourceDocumentIsIndented()
    {
        // arrange
        var document = Parse(
            """
            query (
              $id: ID!
              $first: Int
            ) {
              # the lookup
              productById(id: $id) {
                name(first: $first)
              }
            }
            """);
        var field = FirstField(document);
        var firstDefinition = VariableDefinition(document, "first");
        var writer = new ArrayBufferWriter<byte>();

        // act
        Utf8OperationDocumentBuilder.New(writer)
            .SetName("Batch")
            .AddSharedVariable(firstDefinition)
            .AddRootSelection(field, "Product")
            .AddRootSelection(field, "Product")
            .Complete();

        // assert
        CanonicalText(writer).MatchInlineSnapshot(
            "query Batch($first:Int,$_0_id:ID!,$_1_id:ID!){"
            + "_0_productById:productById(id:$_0_id){..._fusion_body_1} "
            + "_1_productById:productById(id:$_1_id){..._fusion_body_1}} "
            + "fragment _fusion_body_1 on Product{name(first:$first)}");
    }

    [Fact]
    public void Complete_Should_Roundtrip_When_SourceDocumentIsIndented()
    {
        // arrange
        var document = Parse(
            """
            query (
              $id: ID!
              $first: Int
            ) {
              # the lookup
              productById(id: $id) {
                name(first: $first)
              }
            }
            """);
        var field = FirstField(document);
        var firstDefinition = VariableDefinition(document, "first");
        var writer = new ArrayBufferWriter<byte>();
        Utf8OperationDocumentBuilder.New(writer)
            .SetName("Batch")
            .AddSharedVariable(firstDefinition)
            .AddRootSelection(field, "Product")
            .AddRootSelection(field, "Product")
            .Complete();

        // act
        var reparsed = Reparse(writer);
        var operation = FirstOperation(reparsed);

        // assert
        Assert.Equal("Batch", Encoding.UTF8.GetString(operation.Utf8Name));
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
            .AddRootSelection(field, "Query")
            .Complete();

        // assert
        CanonicalText(writer).MatchInlineSnapshot("query($_0_id:ID!){_0_a:a(id:$_0_id)}");
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
            .AddRootSelection(field, "Product")
            .Complete();

        // assert
        CanonicalText(writer).MatchInlineSnapshot("query{_0_productById:productById{name}}");
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
            .AddRootSelection(FirstField(documentA), "Query")
            .AddRootSelection(FirstField(documentB), "Query")
            .Complete();

        // assert
        CanonicalText(writer).MatchInlineSnapshot(
            "query($_0_id:ID!,$_1_id:ID!){_0_a:a(id:$_0_id) _1_b:b(id:$_1_id)}");
    }

    [Fact]
    public void AddRootSelection_Should_EmitFragment_When_TypeConditionIsUtf8()
    {
        // arrange
        var document = Parse("query($id: ID!) { productById(id: $id) { name } }");
        var field = FirstField(document);
        var writer = new ArrayBufferWriter<byte>();

        // act
        Utf8OperationDocumentBuilder.New(writer)
            .AddRootSelection(field, Bytes("Product"))
            .AddRootSelection(field, Bytes("Product"))
            .Complete();

        // assert
        CanonicalText(writer).MatchInlineSnapshot(
            "query($_0_id:ID!,$_1_id:ID!){"
            + "_0_productById:productById(id:$_0_id){..._fusion_body_1} "
            + "_1_productById:productById(id:$_1_id){..._fusion_body_1}} "
            + "fragment _fusion_body_1 on Product{name}");
    }

    [Fact]
    public void AddRootSelection_Should_ShareBody_When_TypeConditionMixesTextAndUtf8()
    {
        // arrange
        var document = Parse("query($id: ID!) { productById(id: $id) { name } }");
        var field = FirstField(document);
        var writer = new ArrayBufferWriter<byte>();

        // act
        Utf8OperationDocumentBuilder.New(writer)
            .AddRootSelection(field, "Product")
            .AddRootSelection(field, Bytes("Product"))
            .Complete();

        // assert
        CanonicalText(writer).MatchInlineSnapshot(
            "query($_0_id:ID!,$_1_id:ID!){"
            + "_0_productById:productById(id:$_0_id){..._fusion_body_1} "
            + "_1_productById:productById(id:$_1_id){..._fusion_body_1}} "
            + "fragment _fusion_body_1 on Product{name}");
    }

    [Fact]
    public void AddRootSelection_Should_DropDescription_When_DefinitionIsRenamed()
    {
        // arrange
        // A description is not part of the variable definition the rows describe, so the
        // canonical output of a renamed definition carries the definition only.
        var document = Parse("query(\"the id\" $id: ID!) { productById(id: $id) { name } }");
        var field = FirstField(document);
        var writer = new ArrayBufferWriter<byte>();

        // act
        Utf8OperationDocumentBuilder.New(writer)
            .AddRootSelection(field, "Product")
            .Complete();

        // assert
        CanonicalText(writer).MatchInlineSnapshot(
            "query($_0_id:ID!){_0_productById:productById(id:$_0_id){name}}");
    }

    [Fact]
    public void AddRootSelection_Should_EmitTypeAndDefault_When_DefinitionIsRenamed()
    {
        // arrange
        var document = Parse(
            "query($ids: [ID!]! = [\"a\"] @tag) { productsByIds(ids: $ids) { name } }");
        var field = FirstField(document);
        var writer = new ArrayBufferWriter<byte>();

        // act
        Utf8OperationDocumentBuilder.New(writer)
            .AddRootSelection(field, "Product")
            .Complete();

        // assert
        CanonicalText(writer).MatchInlineSnapshot(
            "query($_0_ids:[ID!]!=[\"a\"]@tag){"
            + "_0_productsByIds:productsByIds(ids:$_0_ids){name}}");
    }

    [Fact]
    public void Complete_Should_Roundtrip_When_RenamedDefinitionHasTypeAndDefault()
    {
        // arrange
        var document = Parse(
            "query($ids: [ID!]! = [\"a\"] @tag) { productsByIds(ids: $ids) { name } }");
        var writer = new ArrayBufferWriter<byte>();
        Utf8OperationDocumentBuilder.New(writer)
            .AddRootSelection(FirstField(document), "Product")
            .Complete();

        // act
        var reparsed = Reparse(writer);
        var operation = FirstOperation(reparsed);
        var definition = new ArrayBufferWriter<byte>();
        VariableDefinition(reparsed, "_0_ids").Format(definition);

        // assert
        Assert.Equal(1, CountSelections(operation));
        Text(definition).MatchInlineSnapshot(
            """
            $_0_ids:[ID!]!=["a"]@tag
            """);
    }

    [Fact]
    public void SetName_Should_Throw_When_CalledAfterAddRootSelection()
    {
        // arrange
        var document = Parse("query { a { b } }");
        var writer = Utf8OperationDocumentBuilder.New(new ArrayBufferWriter<byte>())
            .AddRootSelection(FirstField(document), "Query");

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
            .AddRootSelection(FirstField(document), "Query");

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
            .AddRootSelection(field, "Query");
        writer.Complete();

        // act
        void Act() => writer.AddRootSelection(field, "Query");

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void Complete_Should_Throw_When_CalledTwice()
    {
        // arrange
        var document = Parse("query { a { b } }");
        var writer = Utf8OperationDocumentBuilder.New(new ArrayBufferWriter<byte>())
            .AddRootSelection(FirstField(document), "Query");
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
    public void Complete_Should_ReturnRentedBuffers_When_ThrowingWithoutRootSelection()
    {
        // arrange
        var document = Parse("query($id: ID!) { productById(id: $id) { name } }");
        var failing = Utf8OperationDocumentBuilder.New(new ArrayBufferWriter<byte>())
            .AddSharedVariable(VariableDefinition(document, "id"));
        var writer = new ArrayBufferWriter<byte>();

        // act
        void Act() => failing.Complete();

        // assert
        // A builder that hands every rented buffer back leaves the next builder unaffected.
        Assert.Throws<InvalidOperationException>(Act);
        Utf8OperationDocumentBuilder.New(writer)
            .AddRootSelection(FirstField(document), "Product")
            .Complete();
        CanonicalText(writer).MatchInlineSnapshot(
            "query($_0_id:ID!){_0_productById:productById(id:$_0_id){name}}");
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
        void Act() => writer.AddRootSelection(field, "Product");

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
        void Act() => writer.AddRootSelection(field, "Product");

        // assert
        Assert.Throws<NotSupportedException>(Act);
    }

    [Fact]
    public void AddRootSelection_Should_LeaveOutputEmpty_When_SelectionHasAlias()
    {
        // arrange
        var document = Parse("query($id: ID!) { alias: productById(id: $id) { name } }");
        var buffer = new ArrayBufferWriter<byte>();
        var writer = Utf8OperationDocumentBuilder.New(buffer, formatAsJsonStringValue: true);

        // act
        void Act() => writer.AddRootSelection(FirstField(document), "Product");

        // assert
        Assert.Throws<NotSupportedException>(Act);
        Assert.Equal(0, buffer.WrittenCount);
    }

    [Fact]
    public void AddRootSelection_Should_Throw_When_SameBodyHasDifferentTypeCondition()
    {
        // arrange
        var document = Parse("query($id: ID!) { productById(id: $id) { name } }");
        var field = FirstField(document);
        var writer = Utf8OperationDocumentBuilder.New(new ArrayBufferWriter<byte>())
            .AddRootSelection(field, "Product");

        // act
        void Act() => writer.AddRootSelection(field, "Article");

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void AddRootSelection_Should_Throw_When_TypeConditionIsEmpty()
    {
        // arrange
        var document = Parse("query($id: ID!) { productById(id: $id) { name } }");
        var field = FirstField(document);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = Utf8OperationDocumentBuilder.New(buffer);

        // act
        void ActWithText() => writer.AddRootSelection(field, string.Empty);
        void ActWithUtf8() => writer.AddRootSelection(field, ReadOnlyMemory<byte>.Empty);

        // assert
        Assert.Throws<ArgumentException>(ActWithText);
        Assert.Throws<ArgumentException>(ActWithUtf8);
        Assert.Equal(0, buffer.WrittenCount);
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
            .AddRootSelection(field, "Query")
            .Complete();

        // assert
        var text = CanonicalText(writer);
        Assert.Contains("$v0:Int", text);
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

    [Fact]
    public void Complete_Should_EmitJsonStringLiteral_When_FormatAsJsonStringValue()
    {
        // arrange
        var document = Parse(
            """
            query($id: ID!, $first: Int) {
              productById(id: $id, tag: "a\"b") { name(first: $first) }
            }
            """);
        var field = FirstField(document);
        var firstDefinition = VariableDefinition(document, "first");
        var writer = new ArrayBufferWriter<byte>();

        // act
        Utf8OperationDocumentBuilder.New(writer, formatAsJsonStringValue: true)
            .SetName("Foo")
            .AddSharedVariable(firstDefinition)
            .AddRootSelection(field, "Product")
            .AddRootSelection(field, "Product")
            .Complete();

        // assert
        Text(writer).MatchInlineSnapshot(
            """
            "query Foo($first:Int,$_0_id:ID!,$_1_id:ID!){_0_productById:productById(id:$_0_id,tag:\"a\\\"b\"){..._fusion_body_1} _1_productById:productById(id:$_1_id,tag:\"a\\\"b\"){..._fusion_body_1}} fragment _fusion_body_1 on Product{name(first:$first)}"
            """);
    }

    [Fact]
    public void Complete_Should_RoundtripDecodedOperation_When_FormatAsJsonStringValue()
    {
        // arrange
        var document = Parse(
            """
            query($id: ID!, $first: Int) {
              productById(id: $id, tag: "a\"b") { name(first: $first) }
            }
            """);
        var field = FirstField(document);
        var firstDefinition = VariableDefinition(document, "first");
        var writer = new ArrayBufferWriter<byte>();
        Utf8OperationDocumentBuilder.New(writer, formatAsJsonStringValue: true)
            .SetName("Foo")
            .AddSharedVariable(firstDefinition)
            .AddRootSelection(field, "Product")
            .AddRootSelection(field, "Product")
            .Complete();

        // act
        using var json = JsonDocument.Parse(writer.WrittenMemory);
        var decoded = json.RootElement.GetString()!;
        var reparsed = Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes(decoded));
        var operation = FirstOperation(reparsed);

        // assert
        Assert.Equal("Foo", Encoding.UTF8.GetString(operation.Utf8Name));
        Assert.Equal(2, CountSelections(operation));
        Assert.Equal(
            "_fusion_body_1",
            Encoding.UTF8.GetString(SingleFragment(reparsed).Utf8Name));
    }

    private static Utf8OperationDocument Parse(string source)
        => Utf8GraphQLOperationParser.Parse(Encoding.UTF8.GetBytes(source));

    private static Utf8OperationDocument Reparse(ArrayBufferWriter<byte> writer)
        => Utf8GraphQLOperationParser.Parse(writer.WrittenSpan.ToArray());

    private static ReadOnlyMemory<byte> Bytes(string value)
        => Encoding.UTF8.GetBytes(value);

    private static string Text(ArrayBufferWriter<byte> writer)
        => Encoding.UTF8.GetString(writer.WrittenSpan);

    private static string CanonicalText(ArrayBufferWriter<byte> writer)
    {
        // Parsing the output proves the canonical glue keeps every token separated.
        _ = Reparse(writer);
        return Encoding.UTF8.GetString(writer.WrittenSpan);
    }

    private static Utf8OperationDefinitionNode FirstOperation(Utf8OperationDocument document)
    {
        var operations = document.GetOperations().GetEnumerator();
        Assert.True(operations.MoveNext());
        return operations.Current;
    }

    private static Utf8FragmentDefinitionNode SingleFragment(Utf8OperationDocument document)
    {
        var fragments = document.GetFragments().GetEnumerator();
        Assert.True(fragments.MoveNext());
        var fragment = fragments.Current;
        Assert.False(fragments.MoveNext());
        return fragment;
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
