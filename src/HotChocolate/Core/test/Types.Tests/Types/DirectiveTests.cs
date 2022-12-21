using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types;

public class DirectiveTests : TypeTestBase
{
    private readonly ITypeInspector _typeInspector = new DefaultTypeInspector();

    [Fact]
    public void ConvertCustomDirectiveToDirectiveNode()
    {
        // arrange
        var schema = CreateSchema();
        var directiveType = schema.GetDirectiveType("Foo");
        var fooDirective = new FooDirective
        {
            Bar = "123",
            Child = new FooChild
            {
                Bar = "456"
            }
        };

        // act
        var directive = Directive.FromDescription(
            directiveType,
            new DirectiveDefinition(
                fooDirective,
                _typeInspector.GetTypeRef(fooDirective.GetType())),
            new object());
        var directiveNode = directive.AsSyntaxNode();

        // assert
        Assert.Equal(directiveType.Name, directiveNode.Name.Value);
        Assert.Collection(directiveNode.Arguments,
            t =>
            {
                Assert.Equal("bar", t.Name.Value);
                Assert.Equal("123", ((StringValueNode)t.Value).Value);
            },
            t =>
            {
                Assert.Equal("child", t.Name.Value);
                Assert.Collection(((ObjectValueNode)t.Value).Fields,
                    x =>
                    {
                        Assert.Equal("bar", x.Name.Value);
                        Assert.Equal("456",
                            ((StringValueNode)x.Value).Value);
                    });
            });
    }

    [Fact]
    public void MapCustomDirectiveToDifferentType()
    {
        // arrange
        var schema = CreateSchema();
        var directiveType = schema.GetDirectiveType("Foo");
        var fooDirective = new FooDirective
        {
            Bar = "123",
            Child = new FooChild
            {
                Bar = "456"
            }
        };

        // act
        var directive = Directive.FromDescription(
            directiveType,
            new DirectiveDefinition(
                fooDirective,
                _typeInspector.GetTypeRef(fooDirective.GetType())),
            new object());
        var mappedObject = directive.AsValue<FooChild>();

        // assert
        Assert.Equal("123", mappedObject.Bar);
    }

    [Fact]
    public void GetArgumentFromCustomDirective()
    {
        // arrange
        var schema = CreateSchema();
        var directiveType = schema.GetDirectiveType("Foo");
        var fooDirective = new FooDirective
        {
            Bar = "123",
            Child = new FooChild
            {
                Bar = "456"
            }
        };

        // act
        var directive = Directive.FromDescription(
            directiveType,
            new DirectiveDefinition(
                fooDirective,
                _typeInspector.GetTypeRef(fooDirective.GetType())),
            new object());
        var barValue = directive.GetArgumentValue<string>("bar");

        // assert
        Assert.Equal("123", barValue);
    }

    [Fact]
    public async Task QueryDirectives_AreAddedToTheSchema()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("foo").Resolve("Bar"))
            .AddType<FooQueryDirectiveType>()
            .BuildSchemaAsync();

        // act
        var printedSchema = schema.Print();

        // assert
        printedSchema.MatchSnapshot();
    }

    [Fact]
    public void ExplicitArguments()
    {
        SchemaBuilder.New()
            .AddDirectiveType<FooDirectiveTypeExplicit>()
            .AddQueryType(d => d
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
        return CreateSchema(b =>
        {
            b.AddDirectiveType<FooDirectiveType>();
            b.AddType<InputObjectType<FooChild>>();
        });
    }

    public class FooQueryDirectiveType
        : DirectiveType<FooDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<FooDirective> descriptor)
        {
            descriptor.Name("Foo");
            descriptor.Location(DirectiveLocation.Query);
        }
    }

    public class FooDirectiveType
        : DirectiveType<FooDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<FooDirective> descriptor)
        {
            descriptor.Name("Foo");
            descriptor.Location(DirectiveLocation.Schema);
        }
    }

    public class FooDirectiveTypeExplicit
        : DirectiveType<FooDirective>
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
