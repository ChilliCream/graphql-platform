using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using StrawberryShake.Transport.WebSockets;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.RecursiveEntitySelfReference;

public class RecursiveEntitySelfReferenceTest : ServerTestBase
{
    public RecursiveEntitySelfReferenceTest(TestServerFactory serverFactory) : base(serverFactory)
    {
    }

    [Fact]
    public async Task SelfReferencingEntity_WithDifferentSelectionSets_ReturnsAllData()
    {
        // arrange
        var ct = new CancellationTokenSource(1200_000).Token;
        using var host = TestServerHelper.CreateServer(
            builder => builder.AddTypeExtension(typeof(QueryType)),
            out var port);

        var entityStore = new EntityStore();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IEntityStore>(entityStore);
        serviceCollection.AddHttpClient(
            RecursiveEntitySelfReferenceClient.ClientName,
            c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
        serviceCollection.AddRecursiveEntitySelfReferenceClient();
        IServiceProvider services = serviceCollection.BuildServiceProvider();
        RecursiveEntitySelfReferenceClient client = services.GetRequiredService<RecursiveEntitySelfReferenceClient>();

        // act
        var response = await client.GetSelfishGuy.ExecuteAsync(ct);

        // assert
        response.Data!.SelfishGuy.MatchSnapshot();
    }

    [QueryType]
    public static class QueryType
    {
        public static Person GetSelfishGuy() => Person.GetSelfishGuy();
    }

    public record Person
    {
        internal static Person GetSelfishGuy()
        {
            var selfishGuy = new Person
            {
                Id = "1",
                FirstName = "John",
                LastName = "Doe",
                Age = 42,
                Phone = "123-456-7890",
                ZipCode = "12345"
            };
            var otherFriend = new Person
            {
                Id = "2",
                FirstName = "Jane",
                LastName = "Smith",
                Age = 41,
                Phone = "555-111-2222",
                ZipCode = "67890"
            };

            selfishGuy.BestFriend = selfishGuy;
            selfishGuy.Friends = new List<Person> { selfishGuy, otherFriend };

            return selfishGuy;
        }

        public string Id { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public int Age { get; set; }
        public string Phone { get; set; } = null!;
        public string ZipCode { get; set; } = null!;
        public Person? BestFriend { get; set; }
        public List<Person>? Friends { get; set; }
    }

    [ExtendObjectType(OperationTypeNames.Query)]
    public class QueryResolvers
    {
        public Person GetSelfishGuy() => Person.GetSelfishGuy();
    }
}
