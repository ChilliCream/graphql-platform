using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class DirectiveTests : TypeTestBase
{
    [Fact]
    public void Directive_AsSyntaxNode()
    {
        // arrange
        var schema = CreateSchema();
        var directiveType = schema.GetDirectiveType("Foo");
        var fooDirective = new FooDirective { Bar = "123", Child = new FooChild { Bar = "456", }, };

        // act
        var directive = new Directive(
            directiveType,
            directiveType.Format(fooDirective),
            fooDirective);
        var directiveNode = directive.AsSyntaxNode();

        // assert
        Assert.Equal(directiveType.Name, directiveNode.Name.Value);
        Assert.Collection(
            directiveNode.Arguments,
            t =>
            {
                Assert.Equal("bar", t.Name.Value);
                Assert.Equal("123", ((StringValueNode)t.Value).Value);
            },
            t =>
            {
                Assert.Equal("child", t.Name.Value);
                Assert.Collection(
                    ((ObjectValueNode)t.Value).Fields,
                    x =>
                    {
                        Assert.Equal("bar", x.Name.Value);
                        Assert.Equal(
                            "456",
                            ((StringValueNode)x.Value).Value);
                    });
            });
    }

    [Fact]
    public void Directive_AsValue_FooDirective()
    {
        // arrange
        var schema = CreateSchema();
        var directiveType = schema.GetDirectiveType("Foo");
        var fooDirective = new FooDirective { Bar = "123", Child = new FooChild { Bar = "456", }, };

        // act
        var syntaxNode = directiveType.Format(fooDirective);
        var value = directiveType.Parse(syntaxNode);
        var directive = new Directive(directiveType, syntaxNode, value);
        var runtimeValue = directive.AsValue<FooDirective>();

        // assert
        Assert.Equal("123", runtimeValue.Bar);
        Assert.Equal("456", runtimeValue.Child.Bar);
    }

    [Fact]
    public void Directive_AsValue_Object()
    {
        // arrange
        var schema = CreateSchema();
        var directiveType = schema.GetDirectiveType("Foo");
        var fooDirective = new FooDirective { Bar = "123", Child = new FooChild { Bar = "456", }, };

        // act
        var syntaxNode = directiveType.Format(fooDirective);
        var value = directiveType.Parse(syntaxNode);
        var directive = new Directive(directiveType, syntaxNode, value);

        // assert
        var runtimeValue = Assert.IsType<FooDirective>(directive.AsValue<object>());
        Assert.Equal("123", runtimeValue.Bar);
        Assert.Equal("456", runtimeValue.Child.Bar);
    }

    [Fact]
    public void Directive_AsValue_Same()
    {
        // arrange
        var schema = CreateSchema();
        var directiveType = schema.GetDirectiveType("Foo");
        var fooDirective = new FooDirective { Bar = "123", Child = new FooChild { Bar = "456", }, };

        // act
        var directive = new Directive(
            directiveType,
            directiveType.Format(fooDirective),
            fooDirective);
        var runtimeValue = directive.AsValue<FooDirective>();

        // assert
        Assert.Same(fooDirective, runtimeValue);
    }

    [Fact]
    public void Directive_GetArgumentValue()
    {
        // arrange
        var schema = CreateSchema();
        var directiveType = schema.GetDirectiveType("Foo");
        var fooDirective = new FooDirective { Bar = "123", Child = new FooChild { Bar = "456", }, };

        // act
        var directive = new Directive(
            directiveType,
            directiveType.Format(fooDirective),
            fooDirective);
        var barValue = directive.GetArgumentValue<string>("bar");

        // assert
        Assert.Equal("123", barValue);
    }

    [Fact]
    public async Task Directive_Query_Directives_Are_Not_Removed_Without_Usage()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolve("Bar"))
            .AddType<FooQueryDirectiveType>()
            .BuildSchemaAsync();

        // act
        var printedSchema = schema.Print();

        // assert
        printedSchema.MatchSnapshot();
    }

    [Fact]
    public void Directive_With_Explicit_Arguments()
    {
        SchemaBuilder.New()
            .AddDirectiveType<FooDirectiveTypeExplicit>()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("abc")
                    .Resolve("def")
                    .Directive(new FooDirective()))
            .Create()
            .Print()
            .MatchSnapshot();
    }

    private static ISchema CreateSchema()
    {
        return CreateSchema(
            b =>
            {
                b.AddDirectiveType<FooDirectiveType>();
                b.AddType<InputObjectType<FooChild>>();
                b.ModifyOptions(o => o.RemoveUnusedTypeSystemDirectives = false);
            });
    }

    public class FooQueryDirectiveType : DirectiveType<FooDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<FooDirective> descriptor)
        {
            descriptor.Name("Foo");
            descriptor.Location(DirectiveLocation.Query);
        }
    }

    public class FooDirectiveType : DirectiveType<FooDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<FooDirective> descriptor)
        {
            descriptor.Name("Foo");
            descriptor.Location(DirectiveLocation.Schema);
        }
    }

    public class FooDirectiveTypeExplicit : DirectiveType<FooDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<FooDirective> descriptor)
        {
            descriptor.Name("Foo");
            descriptor.Location(DirectiveLocation.FieldDefinition);
            descriptor.BindArgumentsExplicitly();
        }
    }

    public class FooDirective
    {
        public string Bar { get; set; }

        public FooChild Child { get; set; }
    }

    public class FooChild
    {
        public string Bar { get; set; }
    }

    public class FooChild2
    {
        public string Bar { get; set; }
    }
}
