namespace HotChocolate.Types.Introspection;

public class SchemaIndexerTests
{
    [Fact]
    public void Index_Should_IndexTypeNames()
    {
        // arrange
        var schema = CreateSchema(d => d
            .Name("Query")
            .Field("hello")
            .Type<StringType>()
            .Resolve("world"));

        // act
        var result = SchemaIndexer.Index(schema);
        var documents = result.Documents;

        // assert
        Assert.Contains(documents, d => d.Coordinate == new SchemaCoordinate("Query"));
    }

    [Fact]
    public void Index_Should_IndexFieldNames()
    {
        // arrange
        var schema = CreateSchema(d => d
            .Name("Query")
            .Field("productName")
            .Type<StringType>()
            .Resolve("test"));

        // act
        var result = SchemaIndexer.Index(schema);
        var documents = result.Documents;

        // assert
        Assert.Contains(documents, d => d.Coordinate == new SchemaCoordinate("Query", "productName"));
    }

    [Fact]
    public void Index_Should_SkipIntrospectionTypes()
    {
        // arrange
        var schema = CreateSchema(d => d
            .Name("Query")
            .Field("hello")
            .Type<StringType>()
            .Resolve("world"));

        // act
        var result = SchemaIndexer.Index(schema);
        var documents = result.Documents;

        // assert
        Assert.DoesNotContain(documents,
            d => d.Coordinate.Name.StartsWith("__"));
    }

    [Fact]
    public void Index_Should_IndexEnumValues()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("status")
                .Type<StatusType>()
                .Resolve(Status.Active))
            .ModifyOptions(o => o.EnableSemanticIntrospection = false)
            .Create();

        // act
        var result = SchemaIndexer.Index(schema);
        var documents = result.Documents;

        // assert
        Assert.Contains(documents, d => d.Coordinate == new SchemaCoordinate("Status", "ACTIVE"));
        Assert.Contains(documents, d => d.Coordinate == new SchemaCoordinate("Status", "INACTIVE"));
    }

    [Fact]
    public void Index_Should_IndexInputObjectFields()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("search")
                .Argument("input", a => a.Type<ProductFilterInputType>())
                .Type<StringType>()
                .Resolve("result"))
            .ModifyOptions(o => o.EnableSemanticIntrospection = false)
            .Create();

        // act
        var result = SchemaIndexer.Index(schema);
        var documents = result.Documents;

        // assert
        Assert.Contains(documents, d => d.Coordinate == new SchemaCoordinate("ProductFilterInput"));
        Assert.Contains(documents, d => d.Coordinate == new SchemaCoordinate("ProductFilterInput", "name"));
    }

    [Fact]
    public void Index_Should_NotIndexDirectiveDefinitions()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("hello")
                .Type<StringType>()
                .Resolve("world"))
            .AddDirectiveType<CachedDirectiveType>()
            .ModifyOptions(o => o.EnableSemanticIntrospection = false)
            .Create();

        // act
        var result = SchemaIndexer.Index(schema);
        var documents = result.Documents;

        // assert — directives have no fetch path and are excluded from search.
        Assert.DoesNotContain(documents, d => d.Coordinate.OfDirective);
    }

    [Fact]
    public void Index_Should_BuildReverseAdjacencyMap()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("product")
                .Type<ProductType>()
                .Resolve(new Product("Test", 9.99m)))
            .ModifyOptions(o => o.EnableSemanticIntrospection = false)
            .Create();

        // act
        var reverseMap = SchemaIndexer.Index(schema).ReverseMap;

        // assert
        // Product is returned by Query.product, so reverse map should map Product -> Query
        Assert.True(reverseMap.ContainsKey("Product"));

        var references = reverseMap["Product"];
        Assert.Contains(references, r => r.Name == "Query" && r.MemberName == "product");
    }

    [Fact]
    public void Index_Should_IncludeDescriptionInText()
    {
        // arrange
        var schema = CreateSchema(d => d
            .Name("Query")
            .Field("product")
            .Description("Gets a product by ID")
            .Type<StringType>()
            .Resolve("test"));

        // act
        var result = SchemaIndexer.Index(schema);
        var documents = result.Documents;

        // assert
        var fieldDoc = documents.First(d => d.Coordinate == new SchemaCoordinate("Query", "product"));
        Assert.Contains("product", fieldDoc.Text);
        Assert.Contains("Gets a product by ID", fieldDoc.Text);
    }

    [Fact]
    public void Index_Should_IndexInterfaceTypeFields()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("node")
                .Type<NodeType>()
                .Resolve(new NodeImpl { Id = "1" }))
            .AddType<NodeType>()
            .AddType<NodeImplType>()
            .ModifyOptions(o => o.EnableSemanticIntrospection = false)
            .Create();

        // act
        var result = SchemaIndexer.Index(schema);
        var documents = result.Documents;

        // assert
        Assert.Contains(documents, d => d.Coordinate == new SchemaCoordinate("Node", "id"));
    }

    private static Schema CreateSchema(Action<IObjectTypeDescriptor> configure)
    {
        return SchemaBuilder.New()
            .AddQueryType(configure)
            .ModifyOptions(o => o.EnableSemanticIntrospection = false)
            .Create();
    }

    // -- Test helper types --

    private enum Status
    {
        Active,
        Inactive
    }

    private sealed class StatusType : EnumType<Status>
    {
        protected override void Configure(IEnumTypeDescriptor<Status> descriptor)
        {
            descriptor.Value(Status.Active).Name("ACTIVE");
            descriptor.Value(Status.Inactive).Name("INACTIVE");
        }
    }

    private sealed class ProductFilterInputType : InputObjectType
    {
        protected override void Configure(IInputObjectTypeDescriptor descriptor)
        {
            descriptor.Name("ProductFilterInput");
            descriptor.Field("name").Type<StringType>();
            descriptor.Field("minPrice").Type<FloatType>();
        }
    }

    private sealed class CachedDirectiveType : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("cached");
            descriptor.Location(DirectiveLocation.Field);
        }
    }

    private record Product(string Name, decimal Price);

    private sealed class ProductType : ObjectType<Product>
    {
        protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
        {
            descriptor.Name("Product");
            descriptor.Field(p => p.Name).Type<StringType>();
            descriptor.Field(p => p.Price).Type<DecimalType>();
        }
    }

    private sealed class NodeImpl
    {
        public string Id { get; set; } = default!;
    }

    private sealed class NodeType : InterfaceType
    {
        protected override void Configure(IInterfaceTypeDescriptor descriptor)
        {
            descriptor.Name("Node");
            descriptor.Field("id").Type<NonNullType<IdType>>();
        }
    }

    private sealed class NodeImplType : ObjectType<NodeImpl>
    {
        protected override void Configure(IObjectTypeDescriptor<NodeImpl> descriptor)
        {
            descriptor.Name("NodeImpl");
            descriptor.Implements<NodeType>();
            descriptor.Field(n => n.Id).Type<NonNullType<IdType>>();
        }
    }
}
