#nullable enable

using System;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate
{
    public class CodeFirstTests
    {
        [Fact]
        public void InferSchemaWithNonNullRefTypes()
        {
            SchemaBuilder.New()
                .AddQueryType<Query>()
                .AddType<Dog>()
                .Create()
                .ToString()
                .MatchSnapshot();
        }

        public class Query
        {
            public string SayHello(string name) =>
                throw new NotImplementedException();

            public Greetings GetGreetings(Greetings greetings) =>
                throw new NotImplementedException();

            public IPet GetPet() =>
                throw new NotImplementedException();

            public Task<IPet?> GetPetOrNull() =>
                throw new NotImplementedException();
        }

        public class Greetings
        {
            public string Name { get; set; } = "Foo";
        }

        public interface IPet
        {
            string? Name { get; }
        }

        public class Dog : IPet
        {
            public string? Name =>
                throw new NotImplementedException();
        }
    }
}
