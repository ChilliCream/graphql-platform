using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class IntegrationTests
{
    [Fact]
    public async Task Schema()
    {
        // arrange
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddSingleton<AuthorRepository>()
            .AddSingleton<BookRepository>()
            .AddSingleton<ChapterRepository>();

        serviceCollection
            .AddGraphQLServer()
            .AddCustomModule()
            .AddGlobalObjectIdentification()
            .AddMutationConventions();

        var services = serviceCollection.BuildServiceProvider();

        // act
        var executor = await services.GetRequiredService<IRequestExecutorResolver>().GetRequestExecutorAsync();

        // assert
        executor.Schema.MatchMarkdownSnapshot();
    }
}
