using System.Text;
using System.Threading.Tasks;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class InterfaceMethodBuilderTests
    {
        [Fact]
        public async Task CreateMethod()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await InterfaceMethodBuilder.New()
                .SetName("GetAbc")
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task CreateMethod_With_One_Parameter()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await InterfaceMethodBuilder.New()
                .SetName("GetAbc")
                .AddParameter(ParameterBuilder.New().SetName("abc").SetType("String"))
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task CreateMethod_With_Two_Parameter()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await InterfaceMethodBuilder.New()
                .SetName("GetAbc")
                .AddParameter(ParameterBuilder.New().SetName("abc").SetType("String"))
                .AddParameter(ParameterBuilder.New().SetName("def").SetType("String").SetDefault())
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task CreateMethod_With_ReturnType()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            // act
            await InterfaceMethodBuilder.New()
                .SetName("GetAbc")
                .SetReturnType("Int32")
                .BuildAsync(writer);

            // assert
            sb.ToString().MatchSnapshot();
        }
    }
}
