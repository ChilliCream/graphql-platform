using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Regressions;

public class Issue_5794
{
    [Fact]
    public async Task Renamed_External_Id_Field_Is_Exposed_As_Id()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Issue5794Query>()
                .AddType<Issue5794MyTypeObjectType>()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              myType {
                id
              }
            }
            """);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);

        using var document = JsonDocument.Parse(result.ToJson());
        var id = document
            .RootElement
            .GetProperty("data")
            .GetProperty("myType")
            .GetProperty("id")
            .GetString();

        Assert.Equal("external", id);
    }

    public sealed class Issue5794Query
    {
        public Issue5794MyType GetMyType()
            => new()
            {
                Id = 1,
                ExternalId = "external"
            };
    }

    public sealed class Issue5794MyType
    {
        public int Id { get; set; }

        public string ExternalId { get; set; } = string.Empty;
    }

    public sealed class Issue5794MyTypeObjectType : ObjectType<Issue5794MyType>
    {
        protected override void Configure(IObjectTypeDescriptor<Issue5794MyType> descriptor)
        {
            descriptor.Field(t => t.Id).Ignore();
            descriptor.Field(t => t.ExternalId).Name("id").Type<StringType>();
        }
    }
}
