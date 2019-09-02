using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate;
using HotChocolate.Language;
using Moq;
using Snapshooter;
using Snapshooter.Xunit;
using StrawberryShake.Generators.CSharp;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using Xunit;
using static System.IO.Path;

namespace StrawberryShake.Generators
{
    public class CodeModelGeneratorTests
    {
        // [InlineData("Simple_Query.graphql")]
        //[InlineData("Spread_Query.graphql")]
        [InlineData("Multiple_Fragments_Query.graphql")]
        [Theory]
        public async Task Generate_Models(string queryFile)
        {
            await ClientGenerator.New()
                .AddSchemaDocumentFromFile(
                    "../../../../Demo/GraphQL/StarWars.graphql")
                .AddQueryDocumentFromFile(
                    "../../../../Demo/GraphQL/Queries.graphql")
                .SetOutput(
                    "../../../../Demo/GraphQL/Generated")
                .BuildAsync();
        }
    }
}
