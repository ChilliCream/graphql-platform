using Xunit;

namespace HotChocolate.Types
{
    public class StringTypeTests
    {
        [Fact]
        public void EnsureStringTypeKindIsCorret()
        {
            // arrange
            StringType type = new StringType();

            // act
            TypeKind kind = type.Kind;

            // assert
            Assert.Equal(TypeKind.Scalar, type.Kind);
        }
    }
}
