using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

public class CostSchemaIndexTests
{
    private static CostSchemaIndex BuildSchemaIndex(string sdl, CostSchemaIndexOptions? options = null)
    {
        var schema = SchemaParser.Parse(sdl);
        return CostSchemaIndex.Create(schema, options ?? new CostSchemaIndexOptions());
    }

    [Fact]
    public void GetTypeWeight_Should_Default_By_Kind_When_No_Cost_Directive()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { book: Book }
            type Book { title: String }
            scalar Foo
            enum Bar { A }
            """);

        // act
        var objectWeight = schemaIndex.GetTypeWeight("Book");
        var scalarWeight = schemaIndex.GetTypeWeight("Foo");
        var enumWeight = schemaIndex.GetTypeWeight("Bar");

        // assert
        Assert.Equal(1.0, objectWeight);
        Assert.Equal(0.0, scalarWeight);
        Assert.Equal(0.0, enumWeight);
    }

    [Fact]
    public void GetTypeWeight_Should_Use_Cost_Directive_When_Present()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { book: Book }
            type Book @cost(weight: "5") { title: String }
            scalar Foo @cost(weight: "2.5")
            """);

        // act
        var objectWeight = schemaIndex.GetTypeWeight("Book");
        var scalarWeight = schemaIndex.GetTypeWeight("Foo");

        // assert
        Assert.Equal(5.0, objectWeight);
        Assert.Equal(2.5, scalarWeight);
    }

    [Fact]
    public void GetFieldWeight_Should_Default_To_One_When_Named_Return_Type_Is_Composite()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { book: Book }
            type Book { title: String author: Author publisher: Publisher }
            type Author { name: String }
            type Publisher { name: String address: Address }
            type Address { zipCode: String }
            """);

        // act & assert
        Assert.Equal(1.0, schemaIndex.GetFieldWeight("Query", "book"));
        Assert.Equal(1.0, schemaIndex.GetFieldWeight("Book", "author"));
        Assert.Equal(1.0, schemaIndex.GetFieldWeight("Book", "publisher"));
        Assert.Equal(1.0, schemaIndex.GetFieldWeight("Publisher", "address"));
    }

    [Fact]
    public void GetFieldWeight_Should_Default_To_Zero_When_Named_Return_Type_Is_Leaf()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { book: Book }
            type Book { title: String tags: [String!] rating: Int }
            """);

        // act & assert
        Assert.Equal(0.0, schemaIndex.GetFieldWeight("Book", "title"));
        Assert.Equal(0.0, schemaIndex.GetFieldWeight("Book", "tags"));
        Assert.Equal(0.0, schemaIndex.GetFieldWeight("Book", "rating"));
    }

    [Fact]
    public void GetFieldWeight_Should_Use_Cost_Directive_When_Present()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { book: Book @cost(weight: "3") }
            type Book { title: String @cost(weight: "-1") }
            """);

        // act
        var fieldWeight = schemaIndex.GetFieldWeight("Query", "book");
        var signedWeight = schemaIndex.GetFieldWeight("Book", "title");

        // assert
        Assert.Equal(3.0, fieldWeight);
        Assert.Equal(-1.0, signedWeight);
    }

    [Fact]
    public void GetArgumentWeight_Should_Default_By_Named_Input_Type_Kind()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { books(filter: BookFilter, limit: Int): [Book] }
            input BookFilter { title: String }
            type Book { title: String }
            """);

        // act
        var inputObjectArg = schemaIndex.GetArgumentWeight("Query", "books", "filter");
        var scalarArg = schemaIndex.GetArgumentWeight("Query", "books", "limit");

        // assert
        Assert.Equal(1.0, inputObjectArg);
        Assert.Equal(0.0, scalarArg);
    }

    [Fact]
    public void GetArgumentWeight_Should_Use_Cost_Directive_When_Present()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { books(limit: Int @cost(weight: "4")): [Book] }
            type Book { title: String }
            """);

        // act
        var weight = schemaIndex.GetArgumentWeight("Query", "books", "limit");

        // assert
        Assert.Equal(4.0, weight);
    }

    [Fact]
    public void GetInputFieldWeight_Should_Default_By_Named_Input_Type_Kind()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { books(filter: BookFilter): [Book] }
            input BookFilter { title: String publisher: PublisherFilter }
            input PublisherFilter { name: String }
            type Book { title: String }
            """);

        // act
        var scalarField = schemaIndex.GetInputFieldWeight("BookFilter", "title");
        var inputObjectField = schemaIndex.GetInputFieldWeight("BookFilter", "publisher");

        // assert
        Assert.Equal(0.0, scalarField);
        Assert.Equal(1.0, inputObjectField);
    }

    [Fact]
    public void GetInputFieldWeight_Should_Use_Cost_Directive_When_Present()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { books(filter: BookFilter): [Book] }
            input BookFilter { title: String @cost(weight: "2") }
            type Book { title: String }
            """);

        // act
        var weight = schemaIndex.GetInputFieldWeight("BookFilter", "title");

        // assert
        Assert.Equal(2.0, weight);
    }

    [Fact]
    public void TryGetDirectiveArguments_Should_ReturnDeclaredArguments_When_DirectiveIsDefined()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            directive @custom(factor: Int @cost(weight: "4"), label: String) on FIELD
            type Query { book: Book }
            type Book { title: String }
            """);

        // act
        var found = schemaIndex.TryGetDirectiveArguments("custom", out var arguments);

        // assert
        Assert.True(found);
        Assert.Equal(
            new DirectiveArgumentDefinition[]
            {
                new("factor", 4.0, HasDefaultValue: false),
                new("label", 0.0, HasDefaultValue: false)
            },
            arguments);
    }

    [Fact]
    public void TryGetDirectiveArguments_Should_ReturnFalse_When_DirectiveIsUnknown()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { book: Book }
            type Book { title: String }
            """);

        // act
        var found = schemaIndex.TryGetDirectiveArguments("doesNotExist", out var arguments);

        // assert
        Assert.False(found);
        Assert.True(arguments.IsDefault);
    }

    [Fact]
    public void GetListSizeMetadata_Should_Return_Null_When_Field_Has_No_ListSize_Usage()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { book: Book }
            type Book { title: String }
            """);

        // act
        var metadata = schemaIndex.GetListSizeMetadata("Query", "book");

        // assert
        Assert.Null(metadata);
    }

    [Fact]
    public void GetListSizeMetadata_Should_Read_All_Arguments_When_Present()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query {
              books(first: Int, last: Int): BookConnection
                @listSize(
                  assumedSize: 50
                  slicingArguments: ["first", "last"]
                  slicingArgumentDefaultValue: 10
                  sizedFields: ["edges", "nodes"])
            }
            type BookConnection { edges: [BookEdge] nodes: [Book] }
            type BookEdge { node: Book }
            type Book { title: String }
            """);

        // act
        var metadata = schemaIndex.GetListSizeMetadata("Query", "books");

        // assert
        Assert.NotNull(metadata);
        Assert.Equal(50.0, metadata.AssumedSize);
        Assert.Equal(new[] { "first", "last" }, metadata.SlicingArguments.ToArray());
        Assert.Equal(10.0, metadata.SlicingArgumentDefaultValue);
        Assert.Equal(new[] { "edges", "nodes" }, metadata.SizedFields.ToArray());
    }

    [Fact]
    public void GetListSizeMetadata_Should_Leave_Absent_Optional_Arguments_Null()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { books(first: Int): [Book] @listSize(slicingArguments: ["first"]) }
            type Book { title: String }
            """);

        // act
        var metadata = schemaIndex.GetListSizeMetadata("Query", "books");

        // assert
        Assert.NotNull(metadata);
        Assert.Null(metadata.AssumedSize);
        Assert.Null(metadata.SlicingArgumentDefaultValue);
        Assert.Empty(metadata.SizedFields);
    }

    [Fact]
    public void GetListSizeMetadata_Should_Accept_A_Bare_String_As_A_Single_Element_List()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query {
              books(first: Int): [Book] @listSize(sizedFields: "edges", slicingArguments: "first")
            }
            type Book { title: String }
            """);

        // act
        var metadata = schemaIndex.GetListSizeMetadata("Query", "books");

        // assert
        Assert.NotNull(metadata);
        Assert.Equal(new[] { "first" }, metadata.SlicingArguments.ToArray());
        Assert.Equal(new[] { "edges" }, metadata.SizedFields.ToArray());
    }

    [Fact]
    public void Create_Should_Throw_When_AssumedSize_Is_Negative()
    {
        // arrange
        const string sdl = """
            type Query { a: [Int] @listSize(assumedSize: -1) }
            """;

        // act
        void Act() => BuildSchemaIndex(sdl);

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void Create_Should_Throw_When_AssumedSize_Is_A_Float_Literal()
    {
        // arrange
        const string sdl = """
            type Query { a: [Int] @listSize(assumedSize: 1.5) }
            """;

        // act
        void Act() => BuildSchemaIndex(sdl);

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void Create_Should_Throw_When_AssumedSize_Is_Null()
    {
        // arrange
        const string sdl = "type Query { a: [Int] @listSize(assumedSize: null) }";

        // act
        void Act() => BuildSchemaIndex(sdl);

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Theory]
    [InlineData("slicingArguments")]
    [InlineData("sizedFields")]
    public void Create_Should_Throw_When_ListSizeName_Is_Invalid(string argumentName)
    {
        // arrange
        var sdl = $"type Query {{ a: [Int] @listSize({argumentName}: [\"not a name\"]) }}";

        // act
        void Act() => BuildSchemaIndex(sdl);

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void RequireOneSlicingArgument_Should_Read_Definitions_Declared_Default_When_Omitted()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            directive @listSize(
              assumedSize: Int
              slicingArguments: [String!]
              slicingArgumentDefaultValue: Int
              sizedFields: [String!]
              requireOneSlicingArgument: Boolean = true
            ) on FIELD_DEFINITION
            type Query { books(first: Int): [Book] @listSize(slicingArguments: ["first"]) }
            type Book { title: String }
            """);

        // act
        var metadata = schemaIndex.GetListSizeMetadata("Query", "books");

        // assert
        Assert.True(metadata!.RequireOneSlicingArgument);
    }

    [Fact]
    public void RequireOneSlicingArgument_Should_Read_False_Declared_Default_When_Omitted()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            directive @listSize(
              assumedSize: Int
              slicingArguments: [String!]
              slicingArgumentDefaultValue: Int
              sizedFields: [String!]
              requireOneSlicingArgument: Boolean = false
            ) on FIELD_DEFINITION
            type Query { books(first: Int): [Book] @listSize(slicingArguments: ["first"]) }
            type Book { title: String }
            """);

        // act
        var metadata = schemaIndex.GetListSizeMetadata("Query", "books");

        // assert
        Assert.False(metadata!.RequireOneSlicingArgument);
    }

    [Fact]
    public void RequireOneSlicingArgument_Should_Read_Spec_Default_True_When_No_Definition_Present()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { books(first: Int): [Book] @listSize(slicingArguments: ["first"]) }
            type Book { title: String }
            """);

        // act
        var metadata = schemaIndex.GetListSizeMetadata("Query", "books");

        // assert
        Assert.True(metadata!.RequireOneSlicingArgument);
    }

    [Fact]
    public void RequireOneSlicingArgument_Should_Use_Usages_Own_Literal_Over_The_Definitions_Default()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            directive @listSize(
              assumedSize: Int
              slicingArguments: [String!]
              slicingArgumentDefaultValue: Int
              sizedFields: [String!]
              requireOneSlicingArgument: Boolean = true
            ) on FIELD_DEFINITION
            type Query {
              books(first: Int): [Book]
                @listSize(slicingArguments: ["first"], requireOneSlicingArgument: false)
            }
            type Book { title: String }
            """);

        // act
        var metadata = schemaIndex.GetListSizeMetadata("Query", "books");

        // assert
        Assert.False(metadata!.RequireOneSlicingArgument);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("1")]
    public void Create_Should_Throw_When_RequireOneSlicingArgument_Is_NotBoolean(string value)
    {
        // arrange
        var sdl = $"type Query {{ a: [Int] @listSize(requireOneSlicingArgument: {value}) }}";

        // act
        void Act() => BuildSchemaIndex(sdl);

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void GetTypeWeight_Should_Be_Max_Over_Member_Object_Types_For_An_Interface()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { publication: Publication }
            interface Publication { title: String }
            type Magazine implements Publication @cost(weight: "7") { title: String }
            type Book implements Publication { title: String }
            """);

        // act
        var weight = schemaIndex.GetTypeWeight("Publication");

        // assert
        Assert.Equal(7.0, weight);
    }

    [Fact]
    public void GetTypeWeight_Should_Be_Max_Over_Member_Object_Types_For_A_Union()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { result: Result }
            union Result = Magazine | Book
            type Magazine @cost(weight: "7") { title: String }
            type Book { title: String }
            """);

        // act
        var weight = schemaIndex.GetTypeWeight("Result");

        // assert
        Assert.Equal(7.0, weight);
    }

    [Fact]
    public void GetTypeWeight_Should_Ignore_An_Interfaces_Own_Cost_Directive()
    {
        // arrange
        // The interface annotation is invalid; only its object types determine the weight.
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { publication: Publication }
            interface Publication @cost(weight: "50") { title: String }
            type Magazine implements Publication @cost(weight: "7") { title: String }
            type Book implements Publication { title: String }
            """);

        // act
        var weight = schemaIndex.GetTypeWeight("Publication");

        // assert
        Assert.Equal(7.0, weight);
    }

    [Fact]
    public void GetTypeWeight_Should_Never_Seed_The_Member_Max_With_Zero()
    {
        // arrange
        // Negative member weights expose a maximum calculation incorrectly initialized to zero.
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { neg: Neg }
            interface Neg { x: Int }
            type NegA implements Neg @cost(weight: "-5") { x: Int }
            type NegB implements Neg @cost(weight: "-3") { x: Int }
            """);

        // act
        var weight = schemaIndex.GetTypeWeight("Neg");

        // assert
        Assert.Equal(-3.0, weight);
    }

    [Fact]
    public void GetTypeWeight_Should_Be_One_When_An_Interface_Has_No_Possible_Types()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { p: Publication }
            interface Publication { title: String }
            """);

        // act
        var weight = schemaIndex.GetTypeWeight("Publication");

        // assert
        Assert.Equal(1.0, weight);
    }

    [Fact]
    public void GetPossibleTypeSet_Should_Contain_Only_Itself_For_An_Object_Type()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { book: Book }
            type Book { title: String }
            """);

        // act
        var possibleTypes = schemaIndex.GetPossibleTypeSet("Book");

        // assert
        Assert.Equal(1, possibleTypes.Count);
        Assert.True(possibleTypes.Contains(schemaIndex.GetObjectTypeIndex("Book")));
    }

    [Fact]
    public void GetPossibleTypeSet_Should_Contain_Every_Implementor_For_An_Interface_Type()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { publication: Publication }
            interface Publication { title: String }
            type Magazine implements Publication { title: String }
            type Book implements Publication { title: String }
            """);

        // act
        var possibleTypes = schemaIndex.GetPossibleTypeSet("Publication");

        // assert
        Assert.Equal(2, possibleTypes.Count);
        Assert.True(possibleTypes.Contains(schemaIndex.GetObjectTypeIndex("Magazine")));
        Assert.True(possibleTypes.Contains(schemaIndex.GetObjectTypeIndex("Book")));
    }

    [Fact]
    public void GetPossibleTypeSet_Should_Contain_Every_Member_For_A_Union_Type()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { result: Result }
            union Result = Magazine | Book
            type Magazine { title: String }
            type Book { title: String }
            """);

        // act
        var possibleTypes = schemaIndex.GetPossibleTypeSet("Result");

        // assert
        Assert.Equal(2, possibleTypes.Count);
        Assert.True(possibleTypes.Contains(schemaIndex.GetObjectTypeIndex("Magazine")));
        Assert.True(possibleTypes.Contains(schemaIndex.GetObjectTypeIndex("Book")));
    }

    [Fact]
    public void PossibleTypeSet_Fingerprint_Should_Not_Depend_On_Member_Order()
    {
        // arrange
        var inOrder = PossibleTypeSet.Create(5, [1, 3, 4]);
        var reordered = PossibleTypeSet.Create(5, [4, 1, 3]);

        // act & assert
        Assert.Equal(inOrder.Fingerprint, reordered.Fingerprint);
        Assert.Equal(inOrder.Count, reordered.Count);
        Assert.Equal(inOrder, reordered);
    }

    [Fact]
    public void PossibleTypeSet_Should_Distinguish_Different_Member_Sets()
    {
        // arrange
        var left = PossibleTypeSet.Create(5, [1, 2]);
        var right = PossibleTypeSet.Create(5, [1, 3]);

        // act & assert
        Assert.NotEqual(left.Fingerprint, right.Fingerprint);
        Assert.NotEqual(left, right);
    }

    [Fact]
    public void ReadWeight_Should_Parse_A_String_Literal_With_Invariant_Culture()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { a: Int @cost(weight: "1.5") }
            """);

        // act
        var weight = schemaIndex.GetFieldWeight("Query", "a");

        // assert
        Assert.Equal(1.5, weight);
    }

    [Fact]
    public void ReadWeight_Should_Tolerate_Int_And_Float_Literals()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex(
            """
            type Query { a: Int @cost(weight: 3) b: Int @cost(weight: 2.5) }
            """);

        // act
        var intWeight = schemaIndex.GetFieldWeight("Query", "a");
        var floatWeight = schemaIndex.GetFieldWeight("Query", "b");

        // assert
        Assert.Equal(3.0, intWeight);
        Assert.Equal(2.5, floatWeight);
    }

    [Fact]
    public void Create_Should_Throw_When_A_Cost_Weight_String_Does_Not_Parse()
    {
        // arrange
        const string sdl = """
            type Query { a: Int @cost(weight: "not-a-number") }
            """;

        // act
        void Act() => BuildSchemaIndex(sdl);

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void Create_Should_Throw_When_A_Cost_Weight_Is_Not_Finite()
    {
        // arrange
        const string sdl = """
            type Query { a: Int @cost(weight: "Infinity") b: Int @cost(weight: 1e400) }
            """;

        // act
        void Act() => BuildSchemaIndex(sdl);

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void Create_Should_Throw_When_A_Cost_Usage_Has_No_Weight_Argument()
    {
        // arrange
        const string sdl = """
            type Query { a: Int @cost }
            """;

        // act
        void Act() => BuildSchemaIndex(sdl);

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void Options_Should_ReturnDetachedValueCopies_When_SchemaIndexIsCreated()
    {
        // arrange
        var schema = SchemaParser.Parse("type Query { field: String }");
        var options = new CostSchemaIndexOptions
        {
            DefaultListSize = 42.0,
            CaseBudget = 17,
            CaseBudgetExceededBehavior = CaseBudgetExceededBehavior.Overestimate
        };

        // act
        var schemaIndex = CostSchemaIndex.Create(schema, options);
        options.DefaultListSize = 99.0;
        options.CaseBudget = 3;
        options.CaseBudgetExceededBehavior = CaseBudgetExceededBehavior.EvaluatePerRequest;
        var returned = schemaIndex.Options;
        returned.DefaultListSize = 101.0;
        returned.CaseBudget = 1;
        returned.CaseBudgetExceededBehavior = CaseBudgetExceededBehavior.EvaluatePerRequest;

        // assert
        Assert.Equal(42.0, schemaIndex.Options.DefaultListSize);
        Assert.Equal(17, schemaIndex.Options.CaseBudget);
        Assert.Equal(CaseBudgetExceededBehavior.Overestimate, schemaIndex.Options.CaseBudgetExceededBehavior);
        Assert.NotSame(returned, schemaIndex.Options);
    }
}
