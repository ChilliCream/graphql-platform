using HotChocolate.Language;

namespace HotChocolate.Types;

public class DirectiveCollectionTests : TypeTestBase
{
    [Fact]
    public void DirectiveOrderIsSignificant()
    {
        // arrange
        var someType = new ObjectType(t =>
            t.Name("Foo")
                .Field("abc")
                .Type<StringType>()
                .Resolve("abc")
                .Directive(new DirectiveNode("foo"))
                .Directive(new DirectiveNode("bar")));

        var foo = new DirectiveType(d => d
            .Name("foo")
            .Location(DirectiveLocation.FieldDefinition));

        var bar = new DirectiveType(d => d
            .Name("bar")
            .Location(DirectiveLocation.FieldDefinition));

        // act
        CreateSchema(b =>
        {
            b.AddType(someType);
            b.AddDirectiveType(foo);
            b.AddDirectiveType(bar);
        });

        // assert
        Assert.Collection(someType.Fields["abc"].Directives,
            t => Assert.Equal("foo", t.Type.Name),
            t => Assert.Equal("bar", t.Type.Name));
    }

    [Fact]
    public void DirectiveIsNotRepeatable()
    {
        // arrange
        var someType = new ObjectType(t =>
            t.Name("Foo")
                .Field("abc")
                .Type<StringType>()
                .Resolve("abc")
                .Directive(new DirectiveNode("foo"))
                .Directive(new DirectiveNode("foo")));

        var foo = new DirectiveType(d =>
            d.Name("foo").Location(DirectiveLocation.FieldDefinition));

        // act
        Action action = () => CreateSchema(b =>
        {
            b.AddType(someType);
            b.AddDirectiveType(foo);
        });

        // assert
        Assert.Collection(
            Assert.Throws<SchemaException>(action).Errors,
                t => Assert.Equal(
                    "The specified directive `@foo` " +
                    "is unique and cannot be added twice.",
                    t.Message));
    }

    [Fact]
    public void DirectiveIsRepeatable()
    {
        // arrange
        var someType = new ObjectType(t =>
            t.Name("Foo")
                .Field("abc")
                .Type<StringType>()
                .Resolve("abc")
                .Directive(new DirectiveNode("foo"))
                .Directive(new DirectiveNode("foo")));

        var foo = new DirectiveType(d => d
            .Name("foo")
            .Location(DirectiveLocation.FieldDefinition)
            .Repeatable());

        // act
        CreateSchema(b =>
        {
            b.AddType(someType);
            b.AddDirectiveType(foo);
        });

        // assert
        Assert.Collection(someType.Fields["abc"].Directives,
            t => Assert.Equal("foo", t.Type.Name),
            t => Assert.Equal("foo", t.Type.Name));
    }

    [Fact]
    public void InvalidLocation()
    {
        // arrange
        var someType = new ObjectType(t => t
            .Name("Foo")
            .Field("abc")
            .Type<StringType>()
            .Resolve("abc")
            .Directive(new DirectiveNode("foo")));

        var foo = new DirectiveType(d => d
            .Name("foo")
            .Location(DirectiveLocation.Object));

        // act
        Action action = () => CreateSchema(b =>
        {
            b.AddType(someType);
            b.AddDirectiveType(foo);
        });

        // assert
        Assert.Collection(Assert.Throws<SchemaException>(action).Errors,
            t => Assert.Equal(
                "The specified directive `@foo` " +
                "is not allowed on the current location " +
                $"`{DirectiveLocation.FieldDefinition}`.",
                t.Message));
    }
}
