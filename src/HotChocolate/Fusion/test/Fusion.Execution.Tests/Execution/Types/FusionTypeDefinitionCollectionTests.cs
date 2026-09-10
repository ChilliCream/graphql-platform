using HotChocolate.Fusion.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Types;

public class FusionTypeDefinitionCollectionTests : FusionTestBase
{
    [Fact]
    public void GetType_Should_Resolve_Type_When_Utf8Name_Matches_Accessible_Type()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
            }
            """);
        var expected = schema.Types.GetType<IObjectTypeDefinition>("Product");

        // act
        var resolved = schema.Types.GetType<IObjectTypeDefinition>("Product"u8);

        // assert
        Assert.Same(expected, resolved);
    }

    [Fact]
    public void GetType_Should_Throw_KeyNotFoundException_When_Utf8Name_Does_Not_Exist()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
            }
            """);

        // act
        void Act() => schema.Types.GetType<IObjectTypeDefinition>("DoesNotExist"u8);

        // assert
        Assert.Throws<KeyNotFoundException>(Act);
    }

    [Fact]
    public void GetType_Should_Throw_KeyNotFoundException_When_Type_Is_Inaccessible_And_Not_Allowed()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              product: Product
              hidden: Hidden @inaccessible
            }

            type Product {
              id: ID!
            }

            type Hidden @inaccessible {
              id: ID!
            }
            """);

        // act
        void Act() => schema.Types.GetType<IObjectTypeDefinition>("Hidden"u8, allowInaccessibleFields: false);

        // assert
        Assert.Throws<KeyNotFoundException>(Act);
    }

    [Fact]
    public void GetType_Should_Resolve_Type_When_Type_Is_Inaccessible_And_Allowed()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              product: Product
              hidden: Hidden @inaccessible
            }

            type Product {
              id: ID!
            }

            type Hidden @inaccessible {
              id: ID!
            }
            """);

        // act
        var resolved = schema.Types.GetType<IObjectTypeDefinition>("Hidden"u8, allowInaccessibleFields: true);

        // assert
        Assert.Equal("Hidden", resolved.Name);
    }

    [Fact]
    public void TryGetType_Should_Return_True_When_Utf8Name_Matches_Accessible_Type()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
            }
            """);
        var expected = schema.Types.GetType<IObjectTypeDefinition>("Product");

        // act
        var found = schema.Types.TryGetType<IObjectTypeDefinition>("Product"u8, out var resolved);

        // assert
        Assert.True(found);
        Assert.Same(expected, resolved);
    }

    [Fact]
    public void TryGetType_Should_Return_False_When_Utf8Name_Does_Not_Exist()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
            }
            """);

        // act
        var found = schema.Types.TryGetType<IObjectTypeDefinition>("DoesNotExist"u8, out var resolved);

        // assert
        Assert.False(found);
        Assert.Null(resolved);
    }

    [Fact]
    public void TryGetType_Should_Return_False_When_Type_Is_Inaccessible_And_Not_Allowed()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              product: Product
              hidden: Hidden @inaccessible
            }

            type Product {
              id: ID!
            }

            type Hidden @inaccessible {
              id: ID!
            }
            """);

        // act
        var found = schema.Types.TryGetType<IObjectTypeDefinition>(
            "Hidden"u8,
            allowInaccessibleFields: false,
            out var resolved);

        // assert
        Assert.False(found);
        Assert.Null(resolved);
    }
}
