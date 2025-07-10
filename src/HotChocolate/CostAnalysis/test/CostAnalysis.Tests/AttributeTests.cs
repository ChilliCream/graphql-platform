using HotChocolate.CostAnalysis.Types;
using HotChocolate.Language.Utilities;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

public sealed class AttributeTests
{
    [Fact]
    public void Cost_ArgumentAttribute_AppliesDirective()
    {
        // arrange & act
        var query = CreateSchema().Types.GetType<ObjectType>(OperationTypeNames.Query);

        var costDirective = query.Fields["examples"]
            .Arguments["_"]
            .Directives
            .Single(d => d.Type.Name == "cost")
            .ToValue<CostDirective>();

        // assert
        Assert.Equal(8.0, costDirective.Weight);
    }

    [Fact]
    public void Cost_EnumTypeAttribute_AppliesDirective()
    {
        // arrange & act
        var exampleEnum = CreateSchema().Types.GetType<EnumType>(nameof(ExampleEnum));

        var costDirective = exampleEnum
            .Directives
            .Single(d => d.Type.Name == "cost")
            .ToValue<CostDirective>();

        // assert
        Assert.Equal(0.0, costDirective.Weight);
    }

    [Fact]
    public void Cost_InputFieldAttribute_AppliesDirective()
    {
        // arrange & act
        var exampleInput = CreateSchema().Types.GetType<InputObjectType>(nameof(ExampleInput));

        var costDirective = exampleInput.Fields["field"]
            .Directives
            .Single(d => d.Type.Name == "cost")
            .ToValue<CostDirective>();

        // assert
        Assert.Equal(-3.0, costDirective.Weight);
    }

    [Fact]
    public void Cost_ObjectFieldAttribute_AppliesDirective()
    {
        // arrange & act
        var query = CreateSchema().Types.GetType<ObjectType>(OperationTypeNames.Query);

        var costDirective = query.Fields["examples"]
            .Directives
            .Single(d => d.Type.Name == "cost")
            .ToValue<CostDirective>();

        // assert
        Assert.Equal(5.0, costDirective.Weight);
    }

    [Fact]
    public void Cost_ObjectTypeAttribute_AppliesDirective()
    {
        // arrange & act
        var example = CreateSchema().Types.GetType<ObjectType>(nameof(Example));

        var costDirective = example.Directives
            .Single(d => d.Type.Name == "cost")
            .ToValue<CostDirective>();

        // assert
        Assert.Equal(10.0, costDirective.Weight);
    }

    [Fact]
    public void Cost_ScalarTypeAttribute_AppliesDirective()
    {
        // arrange & act
        var exampleScalar = CreateSchema().Types.GetType<ExampleScalar>(nameof(ExampleScalar));

        var costDirective = exampleScalar.Directives
            .Single(d => d.Type.Name == "cost")
            .ToValue<CostDirective>();

        // assert
        Assert.Equal(1.0, costDirective.Weight);
    }

    [Fact]
    public void ListSize_ObjectFieldAttribute_AppliesDirective()
    {
        // arrange & act
        var query = CreateSchema().Types.GetType<ObjectType>(OperationTypeNames.Query);

        var costDirective = query.Fields["examples"]
            .Directives
            .Single(d => d.Type.Name == "listSize")
            .ToValue<ListSizeDirective>();

        // assert
        Assert.Equal(10, costDirective.AssumedSize);
        Assert.Equal(["first", "last"], costDirective.SlicingArguments, StringComparer.Ordinal);
        Assert.Equal(["edges", "nodes"], costDirective.SizedFields, StringComparer.Ordinal);
        Assert.False(costDirective.RequireOneSlicingArgument);
    }

    [Fact]
    public void ListSize_ObjectFieldAttribute_AppliesRequireOneSlicingArgumentCorrectly()
    {
        // arrange & act
        var query = CreateSchema().Types.GetType<ObjectType>(OperationTypeNames.Query);

        var listSizeDirective1Sdl = query.Fields["examplesAssumedSizeOnly"]
            .Directives
            .Single(d => d.Type.Name == "listSize")
            .ToSyntaxNode(removeDefaults: false)
            .Print();

        var listSizeDirective2Sdl = query.Fields["examplesRequireOneSlicingArgumentTrue"]
            .Directives
            .Single(d => d.Type.Name == "listSize")
            .ToSyntaxNode(removeDefaults: false)
            .Print();

        var listSizeDirective3Sdl = query.Fields["examplesRequireOneSlicingArgumentFalse"]
            .Directives
            .Single(d => d.Type.Name == "listSize")
            .ToSyntaxNode(removeDefaults: false)
            .Print();

        // assert
        listSizeDirective1Sdl.MatchInlineSnapshot("@listSize(assumedSize: 10)");
        listSizeDirective2Sdl.MatchInlineSnapshot("@listSize(requireOneSlicingArgument: true)");
        listSizeDirective3Sdl.MatchInlineSnapshot("@listSize(requireOneSlicingArgument: false)");
    }

    private static Schema CreateSchema()
    {
        return SchemaBuilder.New()
            .AddQueryType(new ObjectType(d => d.Name(OperationTypeNames.Query)))
            .AddType(typeof(Queries))
            .AddType<ExampleScalar>()
            .AddDirectiveType<CostDirectiveType>()
            .AddDirectiveType<ListSizeDirectiveType>()
            .AddEnumType<ExampleEnum>()
            .Use(next => next)
            .Create();
    }

    [QueryType]
    private static class Queries
    {
        private static readonly List<Example> s_list = [new Example(ExampleEnum.Member)];

        [ListSize(
            AssumedSize = 10,
            SlicingArguments = ["first", "last"],
            SizedFields = ["edges", "nodes"],
            RequireOneSlicingArgument = false)]
        [Cost(5.0)]
        // ReSharper disable once UnusedMember.Local
        public static List<Example> GetExamples([Cost(8.0)] ExampleInput _)
        {
            return s_list;
        }

        [ListSize(AssumedSize = 10)]
        // ReSharper disable once UnusedMember.Local
        public static List<Example> GetExamplesAssumedSizeOnly()
        {
            return s_list;
        }

        [ListSize(RequireOneSlicingArgument = true)]
        // ReSharper disable once UnusedMember.Local
        public static List<Example> GetExamplesRequireOneSlicingArgumentTrue()
        {
            return s_list;
        }

        [ListSize(RequireOneSlicingArgument = false)]
        // ReSharper disable once UnusedMember.Local
        public static List<Example> GetExamplesRequireOneSlicingArgumentFalse()
        {
            return s_list;
        }
    }

    [ObjectType]
    [Cost(10.0)]
    private sealed class Example(ExampleEnum field)
    {
        // ReSharper disable once UnusedMember.Local
        public ExampleEnum Field { get; set; } = field;
    }

    [InputObjectType]
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class ExampleInput(string field)
    {
        [Cost(-3.0)]
        // ReSharper disable once UnusedMember.Local
        public string Field { get; set; } = field;
    }

    [Cost(1.0)]
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class ExampleScalar() : StringType("ExampleScalar");

    [EnumType]
    [Cost(0.0)]
    private enum ExampleEnum { Member }
}
