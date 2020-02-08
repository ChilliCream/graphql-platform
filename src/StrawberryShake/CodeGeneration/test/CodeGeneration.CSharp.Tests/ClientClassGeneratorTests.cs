using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ClientClassGeneratorTests
    {
        [Fact]
        public async Task GenerateModel()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new ClientClassGenerator();

            var descriptor = new ClientClassDescriptor(
                "TestClient",
                "ITestClient",
                "global::StrawberryShake.IOperationExecutorPool",
                "global::StrawberryShake.IOperationExecutor",
                "global::StrawberryShake.IOperationStreamExecutor",
                new []
                {
                    new ClientOperationMethodDescriptor(
                        "GetHero",
                        "global::Demo.GetHeroOperation",
                        false,
                        "global::System.Threading.Tasks.Task<" +
                        "global::StrawberryShale.IOperationResult<global::Demo.IGetHero>>",
                        new[]
                        {
                            new ClientOperationMethodParameterDescriptor(
                                "episode",
                                "Episode",
                                "global::Demo.Episode",
                                true,
                                "NewHope")
                        })
                });

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public void CanHandle()
        {
            // arrange
            var generator = new ClientClassGenerator();

            var descriptor = new ClientClassDescriptor(
                "TestClient",
                "ITestClient",
                "global::StrawberryShake.IOperationExecutorPool",
                "global::StrawberryShake.IOperationExecutor",
                "global::StrawberryShake.IOperationStreamExecutor",
                new []
                {
                    new ClientOperationMethodDescriptor(
                        "episode",
                        "GetHeroOperation",
                        false,
                        "Task<IOperationResult<IGetHero>>",
                        new[]
                        {
                            new ClientOperationMethodParameterDescriptor(
                                "episode",
                                "Episode",
                                "BAZ",
                                true,
                                "foo")
                        })
                });

            // act
            var canHandle = generator.CanHandle(descriptor);

            // assert
            Assert.True(canHandle);
        }
    }
}
