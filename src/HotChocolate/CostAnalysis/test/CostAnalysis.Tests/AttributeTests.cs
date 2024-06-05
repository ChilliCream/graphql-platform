using HotChocolate.CostAnalysis.Attributes;
using HotChocolate.CostAnalysis.Directives;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

public sealed class AttributeTests
{
    [Fact]
    public void Cost_ArgumentAttribute_AppliesDirective()
    {
        // arrange & act
        var query = CreateSchema().GetType<ObjectType>(OperationTypeNames.Query);

        var costDirective = query.Fields["examples"]
            .Arguments["_"]
            .Directives
            .Single(d => d.Type.Name == "cost")
            .AsValue<CostDirective>();

        // assert
        Assert.Equal("8.0", costDirective.Weight);
    }

    [Fact]
    public void Cost_EnumTypeAttribute_AppliesDirective()
    {
        // arrange & act
        var exampleEnum = CreateSchema().GetType<EnumType>(nameof(ExampleEnum));

        var costDirective = exampleEnum
            .Directives
            .Single(d => d.Type.Name == "cost")
            .AsValue<CostDirective>();

        // assert
        Assert.Equal("0.0", costDirective.Weight);
    }

    [Fact]
    public void Cost_InputFieldAttribute_AppliesDirective()
    {
        // arrange & act
        var exampleInput = CreateSchema().GetType<InputObjectType>(nameof(ExampleInput));

        var costDirective = exampleInput.Fields["field"]
            .Directives
            .Single(d => d.Type.Name == "cost")
            .AsValue<CostDirective>();

        // assert
        Assert.Equal("-3.0", costDirective.Weight);
    }

    [Fact]
    public void Cost_ObjectFieldAttribute_AppliesDirective()
    {
        // arrange & act
        var query = CreateSchema().GetType<ObjectType>(OperationTypeNames.Query);

        var costDirective = query.Fields["examples"]
            .Directives
            .Single(d => d.Type.Name == "cost")
            .AsValue<CostDirective>();

        // assert
        Assert.Equal("5.0", costDirective.Weight);
    }

    [Fact]
    public void Cost_ObjectTypeAttribute_AppliesDirective()
    {
        // arrange & act
        var example = CreateSchema().GetType<ObjectType>(nameof(Example));

        var costDirective = example.Directives
            .Single(d => d.Type.Name == "cost")
            .AsValue<CostDirective>();

        // assert
        Assert.Equal("10.0", costDirective.Weight);
    }

    [Fact]
    public void ListSize_ObjectFieldAttribute_AppliesDirective()
    {
        // arrange & act
        var query = CreateSchema().GetType<ObjectType>(OperationTypeNames.Query);

        var costDirective = query.Fields["examples"]
            .Directives
            .Single(d => d.Type.Name == "listSize")
            .AsValue<ListSizeDirective>();

        // assert
        Assert.Equal(10, costDirective.AssumedSize);
        Assert.Equal(["first", "last"], costDirective.SlicingArguments);
        Assert.Equal(["edges", "nodes"], costDirective.SizedFields);
        Assert.False(costDirective.RequireOneSlicingArgument);
    }

    private static ISchema CreateSchema()
    {
        return SchemaBuilder.New()
            .AddQueryType(new ObjectType(d => d.Name(OperationTypeNames.Query)))
            .AddType(typeof(Queries))
            .AddDirectiveType<CostDirectiveType>()
            .AddDirectiveType<ListSizeDirectiveType>()
            .AddEnumType<ExampleEnum>()
            .Use(next => next)
            .Create();
    }

    [QueryType]
    private static class Queries
    {
        [ListSize(
            AssumedSize = 10,
            SlicingArguments = ["first", "last"],
            SizedFields = ["edges", "nodes"],
            RequireOneSlicingArgument = false)]
        [Cost("5.0")]
        // ReSharper disable once UnusedMember.Local
        public static List<Example> GetExamples([Cost("8.0")] ExampleInput _)
        {
            return [new Example(ExampleEnum.Member)];
        }
    }

    [ObjectType]
    [Cost("10.0")]
    private sealed class Example(ExampleEnum field)
    {
        // ReSharper disable once UnusedMember.Local
        public ExampleEnum Field { get; set; } = field;
    }

    [InputObjectType]
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class ExampleInput(string field)
    {
        [Cost("-3.0")]
        // ReSharper disable once UnusedMember.Local
        public string Field { get; set; } = field;
    }

    [EnumType]
    [Cost("0.0")]
    private enum ExampleEnum { Member }
}
