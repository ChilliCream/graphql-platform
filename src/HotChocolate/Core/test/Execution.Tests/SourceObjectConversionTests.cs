using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class SourceObjectConversionTests
{
    [Fact]
    public async Task ConvertSourceObject()
    {
        // arrange
        var conversionTriggered = false;

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .AddTypeConverter<Foo, Baz>(input =>
            {
                conversionTriggered = true;
                return new Baz(qux: input.Bar);
            })
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ foo { qux } }");

        // assert
        Assert.True(
            Assert.IsType<OperationResult>(result).Errors is null,
            "There should be no errors.");
        Assert.True(
            conversionTriggered,
            "The custom converter should have been hit.");
        result.MatchSnapshot();
    }

    [Fact]
    public async Task NoConverter_Specified()
    {
        // arrange
        var schema =
            SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .Create();

        // act
        var request =
            OperationRequestBuilder.New()
                .SetDocument("{ foo { qux } }")
                .Build();

        var result =
            await schema.MakeExecutable().ExecuteAsync(request);

        // assert
        result.ToJson().MatchSnapshot();
    }

    public class Query
    {
        public Foo Foo { get; } = new(bar: "bar");
    }

    public class QueryType : ObjectType<Query>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Field(t => t.Foo).Type<BazType>();
        }
    }

    public class Foo(string bar)
    {
        public string Bar { get; set; } = bar;
    }

    public class Baz(string qux)
    {
        public string Qux { get; set; } = qux;
    }

    public class BazType : ObjectType<Baz>
    {
    }
}
