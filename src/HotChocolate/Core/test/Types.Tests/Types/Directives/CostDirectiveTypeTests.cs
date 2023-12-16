using System.Linq;

namespace HotChocolate.Types.Directives;

public class CostDirectiveTypeTests : TypeTestBase
{
    [Fact]
    public void AnnotateCostToObjectFieldCodeFirst()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("field")
                .Argument("a", a => a.Type<StringType>())
                .Type<StringType>()
                .Cost(5))
            .AddDirectiveType<CostDirectiveType>()
            .Use(_ => _)
            .Create();

        var query = schema.GetType<ObjectType>("Query");
        var directive = query.Fields["field"].Directives.Single(t => t.Type.Name == "cost");
        var obj = directive.AsValue<CostDirective>();
        Assert.Equal(5, obj.Complexity);
    }

    [Fact]
    public void AnnotateCostToObjectFieldCodeFirstOneMultiplier()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("field")
                .Argument("a", a => a.Type<StringType>())
                .Type<StringType>()
                .Cost(5, "a"))
            .AddDirectiveType<CostDirectiveType>()
            .Use(_ => _)
            .Create();

        var query = schema.GetType<ObjectType>("Query");
        var directive = query.Fields["field"].Directives.Single(t => t.Type.Name == "cost");
        var obj = directive.AsValue<CostDirective>();
        Assert.Equal(5, obj.Complexity);
        Assert.Collection(obj.Multipliers, t => Assert.Equal("a", t));
    }

    [Fact]
    public void AnnotateCostToObjectFieldCodeFirstTwoMultiplier()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("field")
                .Argument("a", a => a.Type<StringType>())
                .Type<StringType>()
                .Cost(5, "a", "b"))
            .AddDirectiveType<CostDirectiveType>()
            .Use(_ => _)
            .Create();

        var query = schema.GetType<ObjectType>("Query");
        var directive = query.Fields["field"].Directives
            .Single(t => t.Type.Name == "cost");
        var obj = directive.AsValue<CostDirective>();
        Assert.Equal(5, obj.Complexity);
        Assert.Collection(obj.Multipliers,
            t => Assert.Equal("a", t),
            t => Assert.Equal("b", t));
    }

    [Fact]
    public void AnnotateCostToInterfaceFieldCodeFirst()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("field")
                .Argument("a", a => a.Type<StringType>())
                .Type<StringType>())
            .AddInterfaceType(d => d
                .Name("IQuery")
                .Field("field")
                .Argument("a", a => a.Type<StringType>())
                .Type<StringType>()
                .Cost(5))
            .AddDirectiveType<CostDirectiveType>()
            .Use(_ => _)
            .Create();

        var queryInterface = schema.GetType<InterfaceType>("IQuery");
        var directive = queryInterface.Fields["field"].Directives
            .Single(t => t.Type.Name == "cost");
        var obj = directive.AsValue<CostDirective>();
        Assert.Equal(5, obj.Complexity);
    }

    [Fact]
    public void AnnotateCostToInterfaceFieldCodeFirstOneMultiplier()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("field")
                .Argument("a", a => a.Type<StringType>())
                .Type<StringType>())
            .AddInterfaceType(d => d
                .Name("IQuery")
                .Field("field")
                .Argument("a", a => a.Type<StringType>())
                .Type<StringType>()
                .Cost(5, "a"))
            .AddDirectiveType<CostDirectiveType>()
            .Use(_ => _)
            .Create();

        var queryInterface = schema.GetType<InterfaceType>("IQuery");
        var directive = queryInterface.Fields["field"].Directives
            .Single(t => t.Type.Name == "cost");
        var obj = directive.AsValue<CostDirective>();
        Assert.Equal(5, obj.Complexity);
        Assert.Collection(obj.Multipliers, t => Assert.Equal("a", t));
    }

    [Fact]
    public void AnnotateCostToInterfaceFieldCodeFirstTwoMultiplier()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("field")
                .Argument("a", a => a.Type<StringType>())
                .Type<StringType>())
            .AddInterfaceType(d => d
                .Name("IQuery")
                .Field("field")
                .Argument("a", a => a.Type<StringType>())
                .Type<StringType>()
                .Cost(5, "a", "b"))
            .AddDirectiveType<CostDirectiveType>()
            .Use(_ => _)
            .Create();

        var queryInterface = schema.GetType<InterfaceType>("IQuery");
        var directive = queryInterface.Fields["field"].Directives
            .Single(t => t.Type.Name == "cost");
        var obj = directive.AsValue<CostDirective>();
        Assert.Equal(5, obj.Complexity);
        Assert.Collection(obj.Multipliers,
            t => Assert.Equal("a", t),
            t => Assert.Equal("b", t));
    }

    [Fact]
    public void AnnotateCostToObjectFieldSchemaFirst()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"type Query {
                        field(a: Int): String
                            @cost(complexity: 5 multipliers: [""a""])
                    }")
            .AddDirectiveType<CostDirectiveType>()
            .Use(_ => _)
            .Create();

        var query = schema.GetType<ObjectType>("Query");
        var directive = query.Fields["field"].Directives
            .Single(t => t.Type.Name == "cost");
        var obj = directive.AsValue<CostDirective>();
        Assert.Equal(5, obj.Complexity);
        Assert.Collection(obj.Multipliers,
            t => Assert.Equal("a", t));
    }

    [Fact]
    public void AnnotateCostToInterfaceFieldSchemaFirst()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"
                    type Query {
                        field(a: Int): String
                            @cost(complexity: 5 multipliers: [""a""])
                    }

                    interface IQuery {
                        field(a: Int): String
                            @cost(complexity: 5 multipliers: [""a""])
                    }
                    ")
            .AddDirectiveType<CostDirectiveType>()
            .Use(_ => _)
            .Create();

        var queryInterface = schema.GetType<InterfaceType>("IQuery");
        var directive =
            queryInterface.Fields["field"].Directives.Single(t => t.Type.Name == "cost");
        var obj = directive.AsValue<CostDirective>();
        Assert.Equal(5, obj.Complexity);
        Assert.Collection(obj.Multipliers, t => Assert.Equal("a", t));
    }

    [Fact]
    public void CreateCostDirective()
    {
        // arrange
        // act
        var schema = CreateSchema(b => b
            .AddDirectiveType<CostDirectiveType>()
            .ModifyOptions(o => o.RemoveUnusedTypeSystemDirectives = false));
        var directive = schema.DirectiveTypes.OfType<CostDirectiveType>().FirstOrDefault();

        // assert
        Assert.NotNull(directive);
        Assert.IsType<CostDirectiveType>(directive);
        Assert.Equal("cost", directive.Name);
        Assert.Collection(directive.Arguments,
            t =>
            {
                Assert.Equal("complexity", t.Name);
                Assert.IsType<IntType>(
                    Assert.IsType<NonNullType>(t.Type).Type);
            },
            t =>
            {
                Assert.Equal("multipliers", t.Name);
                Assert.IsType<MultiplierPathType>(
                    Assert.IsType<NonNullType>(
                        Assert.IsType<ListType>(t.Type).ElementType).Type);
            },
            t =>
            {
                Assert.Equal("defaultMultiplier", t.Name);
                Assert.IsType<IntType>(t.Type);
            });
        Assert.Collection(directive.Locations.AsEnumerable(),
            t => Assert.Equal(DirectiveLocation.FieldDefinition, t));
    }
}
