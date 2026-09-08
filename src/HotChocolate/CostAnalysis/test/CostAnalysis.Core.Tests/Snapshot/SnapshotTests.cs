using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

public class SnapshotTests
{
    private static CostSchemaSnapshot BuildSnapshot(string sdl, CostEngineOptions? options = null)
    {
        var schema = SchemaParser.Parse(sdl);
        return CostSchemaSnapshot.Create(schema, options ?? new CostEngineOptions());
    }

    // -- Type weight: kind defaults and @cost overrides ---------------------------------------

    [Fact]
    public void GetTypeWeight_Should_Default_By_Kind_When_No_Cost_Directive()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { book: Book }
            type Book { title: String }
            scalar Foo
            enum Bar { A }
            """);

        // act
        var objectWeight = snapshot.GetTypeWeight("Book");
        var scalarWeight = snapshot.GetTypeWeight("Foo");
        var enumWeight = snapshot.GetTypeWeight("Bar");

        // assert
        Assert.Equal(1.0, objectWeight);
        Assert.Equal(0.0, scalarWeight);
        Assert.Equal(0.0, enumWeight);
    }

    [Fact]
    public void GetTypeWeight_Should_Use_Cost_Directive_When_Present()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { book: Book }
            type Book @cost(weight: "5") { title: String }
            scalar Foo @cost(weight: "2.5")
            """);

        // act
        var objectWeight = snapshot.GetTypeWeight("Book");
        var scalarWeight = snapshot.GetTypeWeight("Foo");

        // assert
        Assert.Equal(5.0, objectWeight);
        Assert.Equal(2.5, scalarWeight);
    }

    // -- Field weight: kind defaults and @cost overrides ---------------------------------------

    [Fact]
    public void GetFieldWeight_Should_Default_To_One_When_Named_Return_Type_Is_Composite()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { book: Book }
            type Book { title: String author: Author publisher: Publisher }
            type Author { name: String }
            type Publisher { name: String address: Address }
            type Address { zipCode: String }
            """);

        // act & assert: unannotated fields returning a composite type default to weight 1
        Assert.Equal(1.0, snapshot.GetFieldWeight("Query", "book"));
        Assert.Equal(1.0, snapshot.GetFieldWeight("Book", "author"));
        Assert.Equal(1.0, snapshot.GetFieldWeight("Book", "publisher"));
        Assert.Equal(1.0, snapshot.GetFieldWeight("Publisher", "address"));
    }

    [Fact]
    public void GetFieldWeight_Should_Default_To_Zero_When_Named_Return_Type_Is_Leaf()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { book: Book }
            type Book { title: String tags: [String!] rating: Int }
            """);

        // act & assert: unannotated fields returning a leaf type, including a list of leaves,
        // default to weight 0 (no ChilliCream list-of-scalars opinion inside the engine)
        Assert.Equal(0.0, snapshot.GetFieldWeight("Book", "title"));
        Assert.Equal(0.0, snapshot.GetFieldWeight("Book", "tags"));
        Assert.Equal(0.0, snapshot.GetFieldWeight("Book", "rating"));
    }

    [Fact]
    public void GetFieldWeight_Should_Use_Cost_Directive_When_Present()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { book: Book @cost(weight: "3") }
            type Book { title: String @cost(weight: "-1") }
            """);

        // act
        var fieldWeight = snapshot.GetFieldWeight("Query", "book");
        var signedWeight = snapshot.GetFieldWeight("Book", "title");

        // assert
        Assert.Equal(3.0, fieldWeight);
        Assert.Equal(-1.0, signedWeight);
    }

    // -- Argument and input field weight: kind defaults and @cost overrides ---------------------

    [Fact]
    public void GetArgumentWeight_Should_Default_By_Named_Input_Type_Kind()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { books(filter: BookFilter, limit: Int): [Book] }
            input BookFilter { title: String }
            type Book { title: String }
            """);

        // act
        var inputObjectArg = snapshot.GetArgumentWeight("Query", "books", "filter");
        var scalarArg = snapshot.GetArgumentWeight("Query", "books", "limit");

        // assert
        Assert.Equal(1.0, inputObjectArg);
        Assert.Equal(0.0, scalarArg);
    }

    [Fact]
    public void GetArgumentWeight_Should_Use_Cost_Directive_When_Present()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { books(limit: Int @cost(weight: "4")): [Book] }
            type Book { title: String }
            """);

        // act
        var weight = snapshot.GetArgumentWeight("Query", "books", "limit");

        // assert
        Assert.Equal(4.0, weight);
    }

    [Fact]
    public void GetInputFieldWeight_Should_Default_By_Named_Input_Type_Kind()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { books(filter: BookFilter): [Book] }
            input BookFilter { title: String publisher: PublisherFilter }
            input PublisherFilter { name: String }
            type Book { title: String }
            """);

        // act
        var scalarField = snapshot.GetInputFieldWeight("BookFilter", "title");
        var inputObjectField = snapshot.GetInputFieldWeight("BookFilter", "publisher");

        // assert
        Assert.Equal(0.0, scalarField);
        Assert.Equal(1.0, inputObjectField);
    }

    [Fact]
    public void GetInputFieldWeight_Should_Use_Cost_Directive_When_Present()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { books(filter: BookFilter): [Book] }
            input BookFilter { title: String @cost(weight: "2") }
            type Book { title: String }
            """);

        // act
        var weight = snapshot.GetInputFieldWeight("BookFilter", "title");

        // assert
        Assert.Equal(2.0, weight);
    }

    // -- Directive-definition argument weight (R-DIRECTIVE-ARG-COST) ----------------------------

    [Fact]
    public void GetDirectiveArgumentWeight_Should_Use_Cost_Directive_When_Present()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            directive @custom(factor: Int @cost(weight: "4"), label: String) on FIELD
            type Query { book: Book }
            type Book { title: String }
            """);

        // act
        var annotated = snapshot.GetDirectiveArgumentWeight("custom", "factor");
        var defaulted = snapshot.GetDirectiveArgumentWeight("custom", "label");

        // assert
        Assert.Equal(4.0, annotated);
        Assert.Equal(0.0, defaulted);
    }

    [Fact]
    public void GetDirectiveArgumentWeight_Should_Return_Zero_When_Directive_Or_Argument_Unknown()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { book: Book }
            type Book { title: String }
            """);

        // act
        var weight = snapshot.GetDirectiveArgumentWeight("doesNotExist", "arg");

        // assert
        Assert.Equal(0.0, weight);
    }

    // -- @listSize metadata -----------------------------------------------------------------

    [Fact]
    public void GetListSizeMetadata_Should_Return_Null_When_Field_Has_No_ListSize_Usage()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { book: Book }
            type Book { title: String }
            """);

        // act
        var metadata = snapshot.GetListSizeMetadata("Query", "book");

        // assert
        Assert.Null(metadata);
    }

    [Fact]
    public void GetListSizeMetadata_Should_Read_All_Arguments_When_Present()
    {
        // arrange
        var snapshot = BuildSnapshot(
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
        var metadata = snapshot.GetListSizeMetadata("Query", "books");

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
        var snapshot = BuildSnapshot(
            """
            type Query { books(first: Int): [Book] @listSize(slicingArguments: ["first"]) }
            type Book { title: String }
            """);

        // act
        var metadata = snapshot.GetListSizeMetadata("Query", "books");

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
        var snapshot = BuildSnapshot(
            """
            type Query {
              books(first: Int): [Book] @listSize(sizedFields: "edges", slicingArguments: "first")
            }
            type Book { title: String }
            """);

        // act
        var metadata = snapshot.GetListSizeMetadata("Query", "books");

        // assert
        Assert.NotNull(metadata);
        Assert.Equal(new[] { "first" }, metadata.SlicingArguments.ToArray());
        Assert.Equal(new[] { "edges" }, metadata.SizedFields.ToArray());
    }

    // -- requireOneSlicingArgument (R-REQUIRE-ONE, R-REQUIRE-ONE-DEFAULT) -----------------------

    [Fact]
    public void RequireOneSlicingArgument_Should_Read_Definitions_Declared_Default_When_Omitted()
    {
        // arrange: the directive definition declares `= true`
        var snapshot = BuildSnapshot(
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
        var metadata = snapshot.GetListSizeMetadata("Query", "books");

        // assert
        Assert.True(metadata!.RequireOneSlicingArgument);
    }

    [Fact]
    public void RequireOneSlicingArgument_Should_Read_False_Declared_Default_When_Omitted()
    {
        // arrange: the directive definition declares `= false`
        var snapshot = BuildSnapshot(
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
        var metadata = snapshot.GetListSizeMetadata("Query", "books");

        // assert
        Assert.False(metadata!.RequireOneSlicingArgument);
    }

    [Fact]
    public void RequireOneSlicingArgument_Should_Read_Spec_Default_True_When_No_Definition_Present()
    {
        // arrange: no `directive @listSize(...)` declared at all (R-MUTABLE-SCHEMA)
        var snapshot = BuildSnapshot(
            """
            type Query { books(first: Int): [Book] @listSize(slicingArguments: ["first"]) }
            type Book { title: String }
            """);

        // act
        var metadata = snapshot.GetListSizeMetadata("Query", "books");

        // assert
        Assert.True(metadata!.RequireOneSlicingArgument);
    }

    [Fact]
    public void RequireOneSlicingArgument_Should_Use_Usages_Own_Literal_Over_The_Definitions_Default()
    {
        // arrange: definition default is true, usage explicitly says false
        var snapshot = BuildSnapshot(
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
        var metadata = snapshot.GetListSizeMetadata("Query", "books");

        // assert
        Assert.False(metadata!.RequireOneSlicingArgument);
    }

    // -- Abstract type weight: max over member object types (spec 7.2, hc-3-mmh.7 edge rule d) --

    [Fact]
    public void GetTypeWeight_Should_Be_Max_Over_Member_Object_Types_For_An_Interface()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { publication: Publication }
            interface Publication { title: String }
            type Magazine implements Publication @cost(weight: "7") { title: String }
            type Book implements Publication { title: String }
            """);

        // act
        var weight = snapshot.GetTypeWeight("Publication");

        // assert: max(Magazine 7, Book default 1) = 7
        Assert.Equal(7.0, weight);
    }

    [Fact]
    public void GetTypeWeight_Should_Be_Max_Over_Member_Object_Types_For_A_Union()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { result: Result }
            union Result = Magazine | Book
            type Magazine @cost(weight: "7") { title: String }
            type Book { title: String }
            """);

        // act
        var weight = snapshot.GetTypeWeight("Result");

        // assert: max(Magazine 7, Book default 1) = 7
        Assert.Equal(7.0, weight);
    }

    [Fact]
    public void GetTypeWeight_Should_Ignore_An_Interfaces_Own_Cost_Directive()
    {
        // arrange: the interface's own @cost is not a valid spec location and is never read; the
        // oracle's abstract-type weight consults only member object types
        var snapshot = BuildSnapshot(
            """
            type Query { publication: Publication }
            interface Publication @cost(weight: "50") { title: String }
            type Magazine implements Publication @cost(weight: "7") { title: String }
            type Book implements Publication { title: String }
            """);

        // act
        var weight = snapshot.GetTypeWeight("Publication");

        // assert
        Assert.Equal(7.0, weight);
    }

    [Fact]
    public void GetTypeWeight_Should_Never_Seed_The_Member_Max_With_Zero()
    {
        // arrange: every member is negatively weighted, so a max seeded at 0 would be wrong
        var snapshot = BuildSnapshot(
            """
            type Query { neg: Neg }
            interface Neg { x: Int }
            type NegA implements Neg @cost(weight: "-5") { x: Int }
            type NegB implements Neg @cost(weight: "-3") { x: Int }
            """);

        // act
        var weight = snapshot.GetTypeWeight("Neg");

        // assert
        Assert.Equal(-3.0, weight);
    }

    [Fact]
    public void GetTypeWeight_Should_Be_One_When_An_Interface_Has_No_Possible_Types()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { p: Publication }
            interface Publication { title: String }
            """);

        // act
        var weight = snapshot.GetTypeWeight("Publication");

        // assert
        Assert.Equal(1.0, weight);
    }

    // -- Possible-type sets: object -> [self], interface/union -> members (R-POSSIBLE-TYPES) ----

    [Fact]
    public void GetPossibleTypeSet_Should_Contain_Only_Itself_For_An_Object_Type()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { book: Book }
            type Book { title: String }
            """);

        // act
        var possibleTypes = snapshot.GetPossibleTypeSet("Book");

        // assert
        Assert.Equal(1, possibleTypes.Count);
        Assert.True(possibleTypes.Contains(snapshot.GetObjectTypeIndex("Book")));
    }

    [Fact]
    public void GetPossibleTypeSet_Should_Contain_Every_Implementor_For_An_Interface_Type()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { publication: Publication }
            interface Publication { title: String }
            type Magazine implements Publication { title: String }
            type Book implements Publication { title: String }
            """);

        // act
        var possibleTypes = snapshot.GetPossibleTypeSet("Publication");

        // assert
        Assert.Equal(2, possibleTypes.Count);
        Assert.True(possibleTypes.Contains(snapshot.GetObjectTypeIndex("Magazine")));
        Assert.True(possibleTypes.Contains(snapshot.GetObjectTypeIndex("Book")));
    }

    [Fact]
    public void GetPossibleTypeSet_Should_Contain_Every_Member_For_A_Union_Type()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { result: Result }
            union Result = Magazine | Book
            type Magazine { title: String }
            type Book { title: String }
            """);

        // act
        var possibleTypes = snapshot.GetPossibleTypeSet("Result");

        // assert
        Assert.Equal(2, possibleTypes.Count);
        Assert.True(possibleTypes.Contains(snapshot.GetObjectTypeIndex("Magazine")));
        Assert.True(possibleTypes.Contains(snapshot.GetObjectTypeIndex("Book")));
    }

    // -- PossibleTypeSet: bitset fingerprint is order-independent --------------------------------

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

    // -- Weight literal parsing (R-DIRECTIVE-READING) --------------------------------------------

    [Fact]
    public void ReadWeight_Should_Parse_A_String_Literal_With_Invariant_Culture()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { a: Int @cost(weight: "1.5") }
            """);

        // act
        var weight = snapshot.GetFieldWeight("Query", "a");

        // assert
        Assert.Equal(1.5, weight);
    }

    [Fact]
    public void ReadWeight_Should_Tolerate_Int_And_Float_Literals()
    {
        // arrange
        var snapshot = BuildSnapshot(
            """
            type Query { a: Int @cost(weight: 3) b: Int @cost(weight: 2.5) }
            """);

        // act
        var intWeight = snapshot.GetFieldWeight("Query", "a");
        var floatWeight = snapshot.GetFieldWeight("Query", "b");

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
        void Act() => BuildSnapshot(sdl);

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
        void Act() => BuildSnapshot(sdl);

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
        void Act() => BuildSnapshot(sdl);

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    // -- Options -----------------------------------------------------------------------------

    [Fact]
    public void Options_Should_Return_The_Instance_Passed_To_Create()
    {
        // arrange
        var schema = SchemaParser.Parse("type Query { field: String }");
        var options = new CostEngineOptions { DefaultListSize = 42.0 };

        // act
        var snapshot = CostSchemaSnapshot.Create(schema, options);

        // assert
        Assert.Same(options, snapshot.Options);
    }
}
