using System;
using Xunit;

namespace HotChocolate.Types.Descriptors
{
    public class ClrTypeReferenceTests
    {
        [InlineData(typeof(string[]), TypeContext.Input, false, null)]
        [InlineData(typeof(string[]), TypeContext.Input, null, false)]
        [InlineData(typeof(string), TypeContext.Input, true, false)]
        [InlineData(typeof(string), TypeContext.None, true, false)]
        [InlineData(typeof(string), TypeContext.Output, true, false)]
        [InlineData(typeof(string), TypeContext.Output, false, true)]
        [InlineData(typeof(string), TypeContext.Output, null, true)]
        [InlineData(typeof(string), TypeContext.Output, true, null)]
        [InlineData(typeof(string), TypeContext.Output, null, null)]
        [Theory]
        public void CreateTypeReference(
            Type clrType,
            TypeContext context,
            bool? isTypeNullable,
            bool? isElementTypeNullable)
        {
            // arrange
            // act
            var typeReference = new ClrTypeReference(
                clrType,
                context,
                isTypeNullable,
                isElementTypeNullable);

            // assert
            Assert.Equal(clrType, typeReference.Type);
            Assert.Equal(context, typeReference.Context);
            Assert.Equal(isTypeNullable, typeReference.IsTypeNullable);
            Assert.Equal(isElementTypeNullable,
                typeReference.IsElementTypeNullable);
        }

    }
}
