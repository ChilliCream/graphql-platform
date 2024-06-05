using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;

namespace HotChocolate.Execution;

public class SourceObjectConversionTests
{
    [Fact]
    public async Task ConvertSourceObject()
    {
        // arrange
        var conversionTriggered = false;

        var executor = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .AddTypeConverter<Foo, Baz>(input =>
            {
                conversionTriggered = true;
                return new Baz { Qux = input.Bar, };
            })
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync()
            .Result;

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
            OperationRequestBuilder.Create()
                .SetDocument("{ foo { qux } }")
                .Build();

        var result =
            await schema.MakeExecutable().ExecuteAsync(request);

        // assert
        result.ToJson().MatchSnapshot();
    }

    public class Query
    {
        public Foo Foo { get; } = new Foo { Bar = "bar", };
    }

    public class QueryType : ObjectType<Query>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Field(t => t.Foo).Type<BazType>();
        }
    }

    public class Foo
    {
        public string Bar { get; set; }
    }

    public class Baz
    {
        public string Qux { get; set; }
    }

    public class BazType : ObjectType<Baz>
    {
    }
}
