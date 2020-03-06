using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class FieldBuilderTests
    {
        [Fact]
        public async Task CreateField()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await FieldBuilder.New()
                .SetName("_foo")
                .SetType("string")
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task CreateField_With_Default_Value()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await FieldBuilder.New()
                .SetName("_foo")
                .SetType("List<string>")
                .SetValue("new List<string>()")
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task CreateField_Use_Default_Initializer()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await FieldBuilder.New()
                .SetName("_foo")
                .SetType("string")
                .UseDefaultInitializer()
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task CreateField_Read_Only()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await FieldBuilder.New()
                .SetName("_foo")
                .SetType("string")
                .SetReadOnly()
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task CreateField_Static()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await FieldBuilder.New()
                .SetName("_foo")
                .SetType("string")
                .SetStatic()
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task CreateField_Static_Read_Only()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await FieldBuilder.New()
                .SetName("_foo")
                .SetType("string")
                .SetReadOnly()
                .SetStatic()
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }
    }
}
