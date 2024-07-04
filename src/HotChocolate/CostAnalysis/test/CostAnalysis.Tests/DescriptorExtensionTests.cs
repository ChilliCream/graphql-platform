using HotChocolate.CostAnalysis.Types;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

public sealed class DescriptorExtensionTests
{
    [Fact]
    public void Cost_ArgumentDescriptor_AppliesDirective()
    {
        // arrange & act
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name(OperationTypeNames.Query)
                .Field("field")
                .Argument("a", a => a.Type<StringType>().Cost(5.0))
                .Type<StringType>())
            .AddDirectiveType<CostDirectiveType>()
            .Use(next => next)
            .Create();

        var query = schema.GetType<ObjectType>(OperationTypeNames.Query);

        var costDirective = query.Fields["field"]
            .Arguments["a"]
            .Directives
            .Single(d => d.Type.Name == "cost")
            .AsValue<CostDirective>();

        // assert
        Assert.Equal(5.0, costDirective.Weight);
    }

    [Fact]
    public void Cost_EnumTypeDescriptor_AppliesDirective()
    {
        // arrange & act
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name(OperationTypeNames.Query)
                .Field("field")
                .Type<StringType>())
            .AddDirectiveType<CostDirectiveType>()
            .AddEnumType(d => d.Name("Example").Cost(5.0).Value("EnumMember1"))
            .AddEnumType<ExampleEnum>(d => d.Cost(10.0))
            .Use(next => next)
            .Create();

        var enumType1 = schema.GetType<EnumType>("Example");
        var directive1 = enumType1.Directives.Single(d => d.Type.Name == "cost");
        var costDirective1 = directive1.AsValue<CostDirective>();

        var enumType2 = schema.GetType<EnumType>(nameof(ExampleEnum));
        var directive2 = enumType2.Directives.Single(d => d.Type.Name == "cost");
        var costDirective2 = directive2.AsValue<CostDirective>();

        // assert
        Assert.Equal(5.0, costDirective1.Weight);
        Assert.Equal(10.0, costDirective2.Weight);
    }

    [Fact]
    public void Cost_InputFieldDescriptor_AppliesDirective()
    {
        // arrange & act
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name(OperationTypeNames.Query)
                .Field("field")
                .Type<StringType>())
            .AddDirectiveType<CostDirectiveType>()
            .AddInputObjectType(
                d => d
                    .Name("input")
                    .Field("field")
                    .Type<StringType>()
                    .Cost(5.0))
            .Use(next => next)
            .Create();

        var input = schema.GetType<InputObjectType>("input");
        var directive = input.Fields["field"].Directives.Single(d => d.Type.Name == "cost");
        var costDirective = directive.AsValue<CostDirective>();

        // assert
        Assert.Equal(5.0, costDirective.Weight);
    }

    [Fact]
    public void Cost_ObjectFieldDescriptor_AppliesDirective()
    {
        // arrange & act
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name(OperationTypeNames.Query)
                .Field("field")
                .Type<StringType>()
                .Cost(5.0))
            .AddDirectiveType<CostDirectiveType>()
            .Use(next => next)
            .Create();

        var query = schema.GetType<ObjectType>(OperationTypeNames.Query);
        var directive = query.Fields["field"].Directives.Single(d => d.Type.Name == "cost");
        var costDirective = directive.AsValue<CostDirective>();

        // assert
        Assert.Equal(5.0, costDirective.Weight);
    }

    [Fact]
    public void Cost_ObjectTypeDescriptor_AppliesDirective()
    {
        // arrange & act
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name(OperationTypeNames.Query)
                .Cost(5.0)
                .Field("field")
                .Type<StringType>())
            .AddDirectiveType<CostDirectiveType>()
            .Use(next => next)
            .Create();

        var query = schema.GetType<ObjectType>(OperationTypeNames.Query);
        var directive = query.Directives.Single(d => d.Type.Name == "cost");
        var costDirective = directive.AsValue<CostDirective>();

        // assert
        Assert.Equal(5.0, costDirective.Weight);
    }

    [Fact]
    public void ListSize_ObjectFieldDescriptor_AppliesDirective()
    {
        // arrange & act
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name(OperationTypeNames.Query)
                .Field("field")
                .Type<ListType<StringType>>()
                .ListSize(
                    assumedSize: 10,
                    slicingArguments: ["first", "last"],
                    sizedFields: ["edges", "nodes"],
                    requireOneSlicingArgument: false))
            .AddDirectiveType<ListSizeDirectiveType>()
            .Use(next => next)
            .Create();

        var query = schema.GetType<ObjectType>(OperationTypeNames.Query);
        var directive = query.Fields["field"].Directives.Single(d => d.Type.Name == "listSize");
        var listSizeDirective = directive.AsValue<ListSizeDirective>();

        // assert
        Assert.Equal(10, listSizeDirective.AssumedSize);
        Assert.Equal(["first", "last"], listSizeDirective.SlicingArguments, StringComparer.Ordinal);
        Assert.Equal(["edges", "nodes"], listSizeDirective.SizedFields, StringComparer.Ordinal);
        Assert.False(listSizeDirective.RequireOneSlicingArgument);
    }

    // ReSharper disable once UnusedMember.Local
    private enum ExampleEnum { Member }
}
