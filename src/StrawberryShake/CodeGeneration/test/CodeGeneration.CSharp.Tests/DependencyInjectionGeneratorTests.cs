using StrawberryShake.Tools.Configuration;
using Xunit;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class DependencyInjectionGeneratorTests
    {
        [Fact]
        public void Default_Query() =>
            AssertResult(
                new AssertSettings { Profiles = { TransportProfile.Default } },
                "query GetPerson { person { name } }",
                "type Query { person: Person }",
                "type Person { id: String! name: String! }",
                "extend schema @key(fields: \"id\")");

        [Fact]
        public void Default_Mutation() =>
            AssertResult(
                new AssertSettings { Profiles = { TransportProfile.Default } },
                "mutation CreatePerson { createPerson(id:1) { name } }",
                "type Query { person: Person }",
                "type Mutation { createPerson(id: Int): Person }",
                "type Person { id: String! name: String! }",
                "extend schema @key(fields: \"id\")");

        [Fact]
        public void Default_Subscription() =>
            AssertResult(
                new AssertSettings { Profiles = { TransportProfile.Default } },
                "subscription onPerson { onPerson { name } }",
                "type Query { person: Person }",
                "type Subscription { onPerson: Person }",
                "type Person { id: String! name: String! }",
                "extend schema @key(fields: \"id\")");

        [Fact]
        public void Default_Combined() =>
            AssertResult(
                new AssertSettings { Profiles = { TransportProfile.Default } },
                "query GetPerson { person { name } }",
                "subscription onPerson { onPerson { name } }",
                "mutation CreatePerson { createPerson(id:1) { name } }",
                "type Query { person: Person }",
                "type Mutation { createPerson(id: Int): Person }",
                "type Subscription { onPerson: Person }",
                "type Person { id: String! name: String! }",
                "extend schema @key(fields: \"id\")");

        [Fact]
        public void Default_InMemory() =>
            AssertResult(
                new AssertSettings
                {
                    Profiles =
                    {
                        new TransportProfile(
                            TransportProfile.DefaultProfileName,
                            TransportType.InMemory)
                    }
                },
                "query GetPerson { person { name } }",
                "subscription onPerson { onPerson { name } }",
                "mutation CreatePerson { createPerson(id:1) { name } }",
                "type Query { person: Person }",
                "type Mutation { createPerson(id: Int): Person }",
                "type Subscription { onPerson: Person }",
                "type Person { id: String! name: String! }",
                "extend schema @key(fields: \"id\")");

        [Fact]
        public void Default_MultiProfile() =>
            AssertResult(
                new AssertSettings
                {
                    Profiles =
                    {
                        TransportProfile.Default,
                        new TransportProfile("InMemory", TransportType.InMemory)
                    }
                },
                "query GetPerson { person { name } }",
                "subscription onPerson { onPerson { name } }",
                "mutation CreatePerson { createPerson(id:1) { name } }",
                "type Query { person: Person }",
                "type Mutation { createPerson(id: Int): Person }",
                "type Subscription { onPerson: Person }",
                "type Person { id: String! name: String! }",
                "extend schema @key(fields: \"id\")");

        [Fact]
        public void Default_DifferentTransportMethods() =>
            AssertResult(
                new AssertSettings
                {
                    Profiles =
                    {
                        new TransportProfile(
                            "Shared",
                            TransportType.InMemory,
                            query: TransportType.Http,
                            mutation: TransportType.WebSocket,
                            subscription: TransportType.InMemory)
                    }
                },
                "query GetPerson { person { name } }",
                "subscription onPerson { onPerson { name } }",
                "mutation CreatePerson { createPerson(id:1) { name } }",
                "type Query { person: Person }",
                "type Mutation { createPerson(id: Int): Person }",
                "type Subscription { onPerson: Person }",
                "type Person { id: String! name: String! }",
                "extend schema @key(fields: \"id\")");
    }
}
