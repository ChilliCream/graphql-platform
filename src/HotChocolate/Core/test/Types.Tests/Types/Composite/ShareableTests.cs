using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Composite;

public static class ShareableTests
{
    [Fact]
    public static async Task PageInfo_Is_Shareable()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query1>()
                .AddTypeExtension(typeof(PageInfoExtensions))
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public static async Task PageInfo_Is_Shareable_Fluent()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query1>()
                .AddTypeExtension(
                    new ObjectTypeExtension(d =>
                    {
                        d.Name("PageInfo");
                        d.Shareable();
                    }))
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public static async Task New_On_PageInfo_Is_Shareable()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query1>()
                .AddTypeExtension(typeof(PageInfoScopedExtensions))
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public static async Task New_On_PageInfo_Is_Shareable_Fluent()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query1>()
                .AddTypeExtension(
                    new ObjectTypeExtension(d =>
                    {
                        d.Name("PageInfo");
                        d.Shareable(scoped: true);
                        d.Field("new").Resolve("bar");
                    }))
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public static async Task Shareable_On_Type_That_Is_Both_Input_And_Output_Type()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query2>()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public static async Task Shareable_On_A_Base_Class()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query3>()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    public class Query1
    {
        [UsePaging]
        public IQueryable<string> GetNames()
            => throw new NotImplementedException();
    }

    public class Query2
    {
        public BothInputAndOutput GetOutput(BothInputAndOutput input)
            => input;
    }

    public class Query3
    {
        public SomeConcreteType GetConcreteType() => new();
    }

    [Shareable]
    public class BothInputAndOutput
    {
        public string? Field { get; set; }
    }

    public class SomeConcreteType : ShareableBaseClass;

    [Shareable]
    public abstract class ShareableBaseClass
    {
        public string? Field { get; set; }
    }

    [Shareable]
    [ExtendObjectType("PageInfo")]
    public static class PageInfoExtensions;

    [Shareable(scoped: true)]
    [ExtendObjectType("PageInfo")]
    public static class PageInfoScopedExtensions
    {
        public static int New() => throw new NotImplementedException();
    }
}
