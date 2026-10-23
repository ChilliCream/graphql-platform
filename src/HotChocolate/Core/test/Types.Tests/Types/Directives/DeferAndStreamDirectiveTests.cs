using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Directives;

public class DeferAndStreamDirectiveTests
{
    [Theory]
    [InlineData("defer")]
    [InlineData("stream")]
    public async Task IfArgument_Should_BeNonNullBooleanWithDefaultTrue_When_DirectiveIsEnabled(string directiveName)
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ModifyOptions(
                    o =>
                    {
                        o.EnableDefer = true;
                        o.EnableStream = true;
                    })
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var argument = schema.DirectiveTypes.Single(t => t.Name == directiveName).Arguments["if"];

        // assert
        Assert.Equal("Boolean!", argument.Type.ToString());
        Assert.Equal("true", argument.DefaultValue?.ToString());
    }

    public class Query
    {
        public string[] Foo => ["foo"];
    }
}
