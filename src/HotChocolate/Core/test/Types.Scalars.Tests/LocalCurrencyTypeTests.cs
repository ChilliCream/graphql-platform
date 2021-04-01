using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class LocalCurrencyTypeTests : ScalarTypeTestBase
    {
        [Fact]
        protected void Schema_WithScalar_IsMatch()
        {
            // arrange
            ISchema schema = BuildSchema<LocalCurrencyType>();

            // act

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void LocalCurrency_EnsureLocalCurrencyTypeKindIsCorrect()
        {
            // arrange
            var type = new LocalCurrencyType();

            // act
            TypeKind kind = type.Kind;

            // assert
            Assert.Equal(TypeKind.Scalar, kind);
        }
    }
}
