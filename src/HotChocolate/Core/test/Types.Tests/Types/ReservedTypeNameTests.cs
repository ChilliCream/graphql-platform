using Xunit;

#nullable enable

namespace HotChocolate.Types
{
    public class ReservedTypeNameTests
    {
        [InlineData("_Any")]
        [InlineData("_FieldSet")]
        [Theory]
        public void Ensure_That_StitchingTypes_CannotBeDeclared(string reservedTypeName)
        {
            void Fail() => SchemaBuilder.New().AddType(new AnyType(reservedTypeName)).Create();
            Assert.Throws<SchemaException>(Fail);
        }
    }
}
