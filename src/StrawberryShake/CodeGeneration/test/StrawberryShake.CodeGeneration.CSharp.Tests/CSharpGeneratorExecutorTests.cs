using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.StarWars;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using Xunit;
using Xunit.Sdk;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class CSharpGeneratorExecutorTests
    {
        [Fact]
        public async Task TestMe()
        {
            // arrange
            ClientModel clientModel =
                await TestHelper.CreateClientModelAsync(
                    @"query GetHero {
                        hero(episode: NEW_HOPE) {
                            name
                            appearsIn
                        }
                    }",
                    "extend schema @key(fields: \"id\")");

            // act
            var documents = new List<CSharpDocument>();
            var generator = new CSharpGeneratorExecutor();
            foreach (CSharpDocument document in generator.Generate(clientModel, "Foo"))
            {
                documents.Add(document);
            }

            // assert
            documents.MatchSnapshot();
        }
    }
}
