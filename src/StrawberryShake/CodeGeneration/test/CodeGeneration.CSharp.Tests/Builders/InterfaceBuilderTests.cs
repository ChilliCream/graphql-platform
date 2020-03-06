using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class InterfaceBuilderTests
    {
        [Fact]
        public async Task Create_Marker_Interface()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await InterfaceBuilder.New()
                .SetName("IMarker")
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Create_Interface_That_Implements_IFoo()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await InterfaceBuilder.New()
                .SetName("IMarker")
                .AddImplements("IFoo")
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Create_Interface_That_Implements_IFoo_IBar()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await InterfaceBuilder.New()
                .SetName("IMarker")
                .AddImplements("IFoo")
                .AddImplements("IBar")
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Create_Interface_With_Property()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await InterfaceBuilder.New()
                .SetName("IMarker")
                .AddProperty(InterfacePropertyBuilder.New()
                    .SetName("Property1")
                    .SetType("Bar"))
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Create_Interface_With_Property_Method()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await InterfaceBuilder.New()
                .SetName("IMarker")
                .AddProperty(InterfacePropertyBuilder.New()
                    .SetName("Property1")
                    .SetType("Bar"))
                .AddMethod(InterfaceMethodBuilder.New()
                    .SetName("Method1")
                    .SetReturnType("Bar"))
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }
    }
}
