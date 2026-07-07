using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.ApolloFederation;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using NameNode = HotChocolate.Language.NameNode;

namespace HotChocolate.Fusion;

public class LookupEntityQueryRewriterTests
{
    private const string CatalogSchema =
        """
        # name: catalog
        schema {
          query: Query
        }

        type Query {
          productById(id: ID!): Product @lookup
        }

        type Product @key(fields: "id") {
          id: ID!
          name: String!
          price: Float!
        }
        """;

    private const string WarehouseSchema =
        """
        # name: warehouse
        schema {
          query: Query
        }

        type Query {
          productBySkuAndPackage(
            sku: String! @is(field: "sku")
            package: String! @is(field: "package")): Product @lookup
        }

        type Product @key(fields: "sku package") {
          sku: String!
          package: String!
          id: ID!
          name: String!
        }
        """;

    private const string ProductsSchema =
        """
        # name: products
        schema {
          query: Query
        }

        type Query {
          products: [Product]
        }

        type Product @key(fields: "id") {
          id: ID!
          name: String!
          price: Int!
        }
        """;

    private const string ShippingSchema =
        """
        # name: shipping
        schema {
          query: Query
        }

        type Query {
          productById(id: ID! @is(field: "id")): Product @lookup @internal
        }

        type Product @key(fields: "id") {
          id: ID!
          shippingEstimate(
            price: Int @require(field: "price")
            currency: String): Int
        }
        """;

    private const string FoosSchema =
        """
        # name: foos
        schema {
          query: Query
        }

        type Query {
          foos: [Foo]
        }

        type Foo @key(fields: "id") {
          id: ID!
          bar: Bar @shareable
        }

        type Bar {
          y: String
        }
        """;

    private const string InterfaceFoosSchema =
        """
        # name: ifoos
        schema {
          query: Query
        }

        type Query {
          foos: [Foo]
        }

        type Foo @key(fields: "id") {
          id: ID!
          bar: IBar @shareable
        }

        interface IBar {
          y: String
        }

        type Bar implements IBar {
          y: String @shareable
        }
        """;

    private const string InterfaceRequireSchema =
        """
        # name: inested
        schema {
          query: Query
        }

        type Query {
          fooById(id: ID! @is(field: "id")): Foo @lookup @internal
        }

        type Foo @key(fields: "id") {
          id: ID!
          bar: IBar @shareable
        }

        interface IBar {
          y: String
        }

        type Bar implements IBar {
          y: String @shareable
          x(y: String @require(field: "y")): Int
        }
        """;

    private const string NestedRequireSchema =
        """
        # name: nested
        schema {
          query: Query
        }

        type Query {
          fooById(id: ID! @is(field: "id")): Foo @lookup @internal
        }

        type Foo @key(fields: "id") {
          id: ID!
          bar: Bar @shareable
        }

        type Bar {
          x(y: String @require(field: "y")): Int
        }
        """;

    [Fact]
    public void Rewrite_Should_LiftSelectionSetIntoEntities_When_SimpleScalarKey()
    {
        // arrange
        var schema = ComposeSchema(CatalogSchema);
        var operation = CreateOperation(
            """
            query GetProduct($__fusion_1_id: ID!) {
              productById(id: $__fusion_1_id) {
                id
                name
                price
              }
            }
            """);

        // act
        var rewritten = LookupEntityQueryRewriter.Rewrite(schema, "catalog", operation);

        // assert
        Assert.Equal("Product", rewritten.EntityTypeName);
        rewritten.Operation.SourceText.MatchInlineSnapshot(
            """
            query($representations: [_Any!]!) {
              _entities(representations: $representations) {
                ... on Product {
                  id
                  name
                  price
                }
              }
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_StripBothKeyArguments_When_CompositeKey()
    {
        // arrange
        var schema = ComposeSchema(WarehouseSchema);
        var operation = CreateOperation(
            """
            query($__fusion_1_sku: String!, $__fusion_2_package: String!) {
              productBySkuAndPackage(sku: $__fusion_1_sku, package: $__fusion_2_package) {
                id
                name
              }
            }
            """);

        // act
        var rewritten = LookupEntityQueryRewriter.Rewrite(schema, "warehouse", operation);

        // assert
        Assert.Equal("Product", rewritten.EntityTypeName);
        rewritten.Operation.SourceText.MatchInlineSnapshot(
            """
            query($representations: [_Any!]!) {
              _entities(representations: $representations) {
                ... on Product {
                  id
                  name
                }
              }
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_StripRequireArgument_And_RetainForwardedArgument_When_InnerFieldHasRequire()
    {
        // arrange
        var schema = ComposeSchema(ProductsSchema, ShippingSchema);
        var operation = CreateOperation(
            """
            query($__fusion_1_id: ID!, $__fusion_2_price: Int!, $__fusion_3_currency: String!) {
              productById(id: $__fusion_1_id) {
                shippingEstimate(price: $__fusion_2_price, currency: $__fusion_3_currency)
              }
            }
            """);

        // act
        var rewritten = LookupEntityQueryRewriter.Rewrite(schema, "shipping", operation);

        // assert
        rewritten.Operation.SourceText.MatchInlineSnapshot(
            """
            query($representations: [_Any!]!) {
              _entities(representations: $representations) {
                ... on Product {
                  shippingEstimate(currency: $__fusion_3_currency)
                }
              }
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_StripRequireArgumentInsideInlineFragment_When_FragmentIsInAbstractSelection()
    {
        // arrange
        // 'bar' is interface-typed, so the '@require' argument on 'Bar.x' is only
        // reachable through the inline fragment's type condition.
        var schema = ComposeSchema(InterfaceFoosSchema, InterfaceRequireSchema);
        var operation = CreateOperation(
            """
            query($__fusion_1_id: ID!, $__fusion_2_y: String!) {
              fooById(id: $__fusion_1_id) {
                bar {
                  ... on Bar {
                    x(y: $__fusion_2_y)
                  }
                }
              }
            }
            """);

        // act
        var rewritten = LookupEntityQueryRewriter.Rewrite(schema, "inested", operation);

        // assert
        rewritten.Operation.SourceText.MatchInlineSnapshot(
            """
            query($representations: [_Any!]!) {
              _entities(representations: $representations) {
                ... on Foo {
                  bar {
                    ... on Bar {
                      x
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_RetainForwardedArgument_When_RequiringFieldIsInsideInlineFragment()
    {
        // arrange
        var schema = ComposeSchema(ProductsSchema, ShippingSchema);
        var operation = CreateOperation(
            """
            query($__fusion_1_id: ID!, $__fusion_2_price: Int!, $__fusion_3_currency: String!) {
              productById(id: $__fusion_1_id) {
                ... on Product {
                  shippingEstimate(price: $__fusion_2_price, currency: $__fusion_3_currency)
                }
              }
            }
            """);

        // act
        var rewritten = LookupEntityQueryRewriter.Rewrite(schema, "shipping", operation);

        // assert
        rewritten.Operation.SourceText.MatchInlineSnapshot(
            """
            query($representations: [_Any!]!) {
              _entities(representations: $representations) {
                ... on Product {
                  ... on Product {
                    shippingEstimate(currency: $__fusion_3_currency)
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void RepresentationShapeBuilder_Should_NestRequireUnderStructuralField_When_ComposedRequireIsNested()
    {
        // arrange
        var schema = ComposeSchema(FoosSchema, NestedRequireSchema);
        var operation = CreateOperation(
            """
            query($__fusion_1_id: ID!, $__fusion_2_y: String!) {
              fooById(id: $__fusion_1_id) {
                bar {
                  x(y: $__fusion_2_y)
                }
              }
            }
            """);
        var rewritten = LookupEntityQueryRewriter.Rewrite(schema, "nested", operation);
        var requirements = new[]
        {
            CreateRequirement(schema, "__fusion_1_id", new FieldSelectionMapParser("id").Parse()),
            CreateRequirement(schema, "__fusion_2_y", GetRequireMap(schema, "nested", "Bar", "x", "y"))
        };

        // act
        var shape = RepresentationShapeBuilder.Build(rewritten.LookupField, requirements);

        // assert
        RenderShape(shape).ToString(indented: true).MatchInlineSnapshot(
            """
            {
              id
              bar {
                y
              }
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_Throw_When_RootFieldIsNotAResolvableLookup()
    {
        // arrange
        // 'unknownLookup' parses as a root field but matches no lookup in the source schema.
        var schema = ComposeSchema(CatalogSchema);
        var operation = CreateOperation(
            """
            query {
              unknownLookup {
                id
              }
            }
            """);

        // act
        void Act() => LookupEntityQueryRewriter.Rewrite(schema, "catalog", operation);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal(
            "No lookup matching the root field 'unknownLookup' was found "
            + "in the source schema 'catalog'.",
            exception.Message);
    }

    [Fact]
    public void Rewrite_Should_Throw_When_OperationHasNoRootField()
    {
        // arrange
        // The first selection is an inline fragment, so the operation has no root lookup field.
        var schema = ComposeSchema(CatalogSchema);
        var operation = CreateOperation(
            """
            query {
              ... on Query {
                productById(id: "1") {
                  id
                }
              }
            }
            """);

        // act
        void Act() => LookupEntityQueryRewriter.Rewrite(schema, "catalog", operation);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal(
            "The lookup operation does not contain a root lookup field.",
            exception.Message);
    }

    // Note: ResolveLookup's disambiguation (the '!IsInternal' preference and the multi-candidate
    // ArgumentNamesMatch selection) is exercised only indirectly. The '@internal' shipping schema
    // above drives the internal-lookup branch, but a single source schema cannot expose two lookups
    // that share a root field name through the composer (duplicate root field names are invalid
    // GraphQL), so the multi-candidate tie-break cannot be constructed realistically here.

    private static OperationSourceText CreateOperation(string sourceText)
        => new("Op", OperationType.Query, sourceText, "hash");

    private static OperationRequirement CreateRequirement(
        FusionSchemaDefinition schema,
        string key,
        IValueSelectionNode map)
        => new(
            key,
            new NamedTypeNode("String"),
            SelectionPath.Root,
            map);

    private static IValueSelectionNode GetRequireMap(
        FusionSchemaDefinition schema,
        string schemaName,
        string typeName,
        string fieldName,
        string argumentName)
    {
        var type = schema.Types.GetType<FusionObjectTypeDefinition>(typeName);
        var field = type.Fields[fieldName];

        if (!field.Sources.TryGetMember(schemaName, out var sourceField)
            || sourceField.Requirements is not { } requirements)
        {
            throw new InvalidOperationException(
                $"The field '{typeName}.{fieldName}' has no requirements in '{schemaName}'.");
        }

        for (var i = 0; i < requirements.Arguments.Length; i++)
        {
            if (string.Equals(requirements.Arguments[i].Name, argumentName, StringComparison.Ordinal)
                && requirements.Fields[i] is { } map)
            {
                return map;
            }
        }

        throw new InvalidOperationException(
            $"The argument '{argumentName}' carries no requirement.");
    }

    private static SelectionSetNode RenderShape(List<RepresentationShapeNode> level)
        => new(level.Select(RenderShapeNode).ToList<ISelectionNode>());

    private static FieldNode RenderShapeNode(RepresentationShapeNode node)
        => new(
            location: null,
            new NameNode(node.Name),
            alias: null,
            directives: [],
            arguments: [],
            node.Children is null ? null : RenderShape(node.Children));

    private static FusionSchemaDefinition ComposeSchema(params string[] schemas)
    {
        var sourceSchemas = new List<SourceSchemaText>();
        var autoName = 'a';

        foreach (var schema in schemas)
        {
            string name;
            string sourceText;

            var lines = schema.Split(["\r\n", "\n"], StringSplitOptions.None);

            if (lines.Length > 0 && lines[0].StartsWith("# name:"))
            {
                name = lines[0]["# name:".Length..].Trim();
                sourceText = string.Join(Environment.NewLine, lines.Skip(1));
            }
            else
            {
                name = autoName.ToString();
                autoName++;
                sourceText = schema;
            }

            sourceSchemas.Add(new SourceSchemaText(name, sourceText));
        }

        var composer = new SchemaComposer(sourceSchemas, new SchemaComposerOptions(), new CompositionLog());
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return FusionSchemaDefinition.Create(result.Value.ToSyntaxNode());
    }
}
