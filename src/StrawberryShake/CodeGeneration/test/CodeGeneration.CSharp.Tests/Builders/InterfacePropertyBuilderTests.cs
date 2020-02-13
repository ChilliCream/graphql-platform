using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class InterfacePropertyBuilderTests
    {
        [Fact]
        public async Task Create_ReadOnly_Property()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await InterfacePropertyBuilder.New()
                .SetName("Foo")
                .SetType("Bar")
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Create_Writable_Property()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await InterfacePropertyBuilder.New()
                .SetName("Foo")
                .SetType("Bar")
                .MakeSettable()
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Create_Internal_Writable_Property()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await InterfacePropertyBuilder.New()
                .SetName("Foo")
                .SetType("Bar")
                .MakeSettable()
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }
    }
}
