using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class DocumentGeneratorTests
    {
        [Fact]
        public async Task GenerateModel()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new DocumentGenerator();

            var descriptor = new DocumentDescriptor(
                "Queries",
                "Demo",
                new byte[] { 1,2 ,3 },
                new byte[] { 4,5 ,6 },
                new byte[] { 7,8 ,9 },
                @"type Query {
                    s: String
                }");

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public void CanHandle()
        {
            // arrange
            var generator = new DocumentGenerator();

            var descriptor = new DocumentDescriptor(
                "Queries",
                "Demo",
                new byte[] { 1,2 ,3 },
                new byte[] { 4,5 ,6 },
                new byte[] { 7,8 ,9 },
                @"type Query {
                    s: String
                }");

            // act
            var canHandle = generator.CanHandle(descriptor);

            // assert
            Assert.True(canHandle);
        }
    }
}
