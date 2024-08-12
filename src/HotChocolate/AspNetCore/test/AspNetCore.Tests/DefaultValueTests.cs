using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace HotChocolate.AspNetCore;

public class DefaultValueTests
{
    public class MyInputObjectOut
    {
        public int Result { get; set; }
    }

    public class MyInputObject
    {
        [DefaultValue(500)]
        public Optional<int> ValuesToRetrieveInBatch { get; set; }
    }

    public class Queries
    {
        public string Hello() => "Hello World";
    }

    public class Mutations
    {
        public MyInputObjectOut DoSomething(MyInputObject input) =>
            new MyInputObjectOut() { Result = input.ValuesToRetrieveInBatch.Value };
    }

    [Fact]
    public async Task DefaultValueTests_Simple()
    {
        // Arrange
        var services = new ServiceCollection();
        services
            .AddGraphQLServer()
            .AddQueryType<Queries>()
            .AddMutationType<Mutations>();

        var serviceProvider = services.BuildServiceProvider();
        var executorResolver = serviceProvider.GetRequiredService<IRequestExecutorResolver>();
        var executor = await executorResolver.GetRequestExecutorAsync();

        // Act
        var result = await executor.ExecuteAsync("mutation{ doSomething(input: { }) { result } }");

        // Extract the data from the result
        var jsonResult = result.ToJson();

        // Parse the JSON result and extract the 'result' value
        var jObject = JObject.Parse(jsonResult);
        var actualResult = jObject["data"]!["doSomething"]!["result"]!.Value<int>();

        // Assert
        Assert.Equal(500, actualResult);
    }
}
