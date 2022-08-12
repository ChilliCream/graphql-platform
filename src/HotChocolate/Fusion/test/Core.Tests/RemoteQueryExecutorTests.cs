using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Utilities;
using HotChocolate.Language;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Fusion;

public class RemoteQueryExecutorTests
{
    private readonly TestServerFactory _testServerFactory = new();

    [Fact]
    public async Task Do()
    {
        // arrange
        using var server1 = _testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<Repository1>()
                .AddGraphQLServer()
                .AddQueryType<Query1>(),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));

        using var server2 = _testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<Repository2>()
                .AddGraphQLServer()
                .AddQueryType<Query2>(),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));

        var clients = new Dictionary<string, Func<HttpClient>>
        {
            {
                "a",
                () =>
                {
                    var httpClient = server1.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    return httpClient;
                }
            },
            {
                "b",
                () =>
                {
                    var httpClient = server2.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    return httpClient;
                }
            },
        };

        var clientFactory = new MockHttpClientFactory(clients);

        const string sdl = @"
            type Query {
                personById(id: ID!) : Person
            }

            type Person {
                id: ID!
                name: String!
                bio: String
                friends: [Person!]
            }";

        const string serviceConfiguration = @"
            type Query {
              personById(id: ID!): Person
                @variable(name: ""personId"", argument: ""id"")
                @fetch(from: ""a"", select: ""personById(id: $personId) { ... Person }"")
                @fetch(from: ""b"", select: ""personById(id: $personId) { ... Person }"")
            }

            type Person
              @variable(name: ""personId"", select: ""id"" from: ""a"" type: ""Int!"")
              @variable(name: ""personId"", select: ""id"" from: ""b"" type: ""Int!"")
              @fetch(from: ""a"", select: ""personById(id: $personId) { ... Person }"")
              @fetch(from: ""b"", select: ""personById(id: $personId) { ... Person }"") {

              id: ID!
                @bind(to: ""a"")
                @bind(to: ""b"")
                @bind(to: ""c"")
              name: String!
                @bind(to: ""a"")
              bio: String
                @bind(to: ""b"")

              friends: [Person!]
                @bind(to: ""a"")
            }

            schema
              @httpClient(name: ""a"" baseAddress: ""https://a/graphql"")
              @httpClient(name: ""b"" baseAddress: ""https://b/graphql"") {
              query: Query
            }";

        var request = Parse(
            @"query GetPersonById {
                personById(id: 4) {
                    name
                    friends {
                        name
                        bio
                    }
                }
            }");

        var executor = await new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(clientFactory)
            .AddGraphQLServer()
            .AddGraphQLGateway(serviceConfiguration, sdl)
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            QueryRequestBuilder
                .New()
                .SetQuery(request)
                .Create());

        // assert
        var index = 0;
        var snapshot = new Snapshot();
        var formatter = new JsonQueryResultFormatter(indented: true);

        snapshot.Add(request, "User Request");

        /*
        foreach (var executionNode in queryPlan.ExecutionNodes)
        {
            if (executionNode is RequestNode rn)
            {
                snapshot.Add(rn.Handler.Document, $"Request {++index}");
            }
        }
        */

        snapshot.Add(formatter.Format((QueryResult)result), "Result");

        await snapshot.MatchAsync();
    }

    public class MockHttpClientFactory : IHttpClientFactory
    {
        private readonly Dictionary<string, Func<HttpClient>> _clients;

        public MockHttpClientFactory(Dictionary<string, Func<HttpClient>> clients)
        {
            _clients = clients;
        }

        public HttpClient CreateClient(string name)
            => _clients[name].Invoke();
    }

    public class Query1
    {
        public Person1? GetPersonById(int id, [Service] Repository1 repository)
            => repository.GetPersonById(id);
    }

    public class Query2
    {
        public Person2? GetPersonById(int id, [Service] Repository2 repository)
            => repository.GetPersonById(id);
    }

    public class Repository1
    {
        private readonly Dictionary<int, Person1> _store = new();

        public Repository1()
        {
            var person1 = new Person1(1, "Pascal", Array.Empty<Person1>());
            var person2 = new Person1(2, "Michael", Array.Empty<Person1>());
            var person3 = new Person1(3, "Martin", Array.Empty<Person1>());
            var person4 = new Person1(4, "Rafi", new[] { person1, person2, person3 });

            _store.Add(person1.Id, person1);
            _store.Add(person2.Id, person2);
            _store.Add(person3.Id, person3);
            _store.Add(person4.Id, person4);
        }

        public Person1? GetPersonById(int id)
            => _store.TryGetValue(id, out var p) ? p : null;
    }

    public class Repository2
    {
        private readonly Dictionary<int, Person2> _store = new();

        public Repository2()
        {
            var person1 = new Person2(1, "Foo");
            var person2 = new Person2(2, "Bar");
            var person3 = new Person2(3, "Baz");
            var person4 = new Person2(4, "Qux");

            _store.Add(person1.Id, person1);
            _store.Add(person2.Id, person2);
            _store.Add(person3.Id, person3);
            _store.Add(person4.Id, person4);
        }

        public Person2? GetPersonById(int id)
            => _store.TryGetValue(id, out var p) ? p : null;
    }

    public record Person1(int Id, string Name, IReadOnlyList<Person1> Friends);

    public record Person2(int Id, string Bio);
}
