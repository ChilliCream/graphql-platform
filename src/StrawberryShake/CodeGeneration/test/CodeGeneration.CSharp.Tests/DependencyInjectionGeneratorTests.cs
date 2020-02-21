using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class DependencyInjectionGeneratorTests
    {
        [Fact]
        public async Task GenerateModel()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new DependencyInjectionGenerator();

            var descriptor = new DependencyInjectionDescriptor(
                "StarWarsClientServiceCollectionExtensions",
                "Demo",
                "StarWarsClient",
                "global::Demo.StarWarsClient",
                "global::Demo.IStarWarsClient",
                true,
                new List<string> { "Abc" },
                new List<string> { "Def" });

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateModel_Without_Subscription()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new DependencyInjectionGenerator();

            var descriptor = new DependencyInjectionDescriptor(
                "StarWarsClientServiceCollectionExtensions",
                "Demo",
                "StarWarsClient",
                "global::Demo.StarWarsClient",
                "global::Demo.IStarWarsClient",
                false,
                new List<string> { "Abc" },
                new List<string> { "Def" });

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public void CanHandle()
        {
            // arrange
            var generator = new DependencyInjectionGenerator();

            var descriptor = new DependencyInjectionDescriptor(
                "StarWarsClientServiceCollectionExtensions",
                "Demo",
                "StarWarsClient",
                "global::Demo.StarWarsClient",
                "global::Demo.IStarWarsClient",
                true,
                new List<string> { "Abc" },
                new List<string> { "Def" });

            // act
            var canHandle = generator.CanHandle(descriptor);

            // assert
            Assert.True(canHandle);
        }
    }
}
