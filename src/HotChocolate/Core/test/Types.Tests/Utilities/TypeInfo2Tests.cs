using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using HotChocolate.Internal;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Utilities
{
    public class TypeInfo2Tests
    {
        [InlineData(typeof(List<int?>), "String")]

        [Theory]
        public void CreateTypeInfoFromReferenceType(
            Type clrType,
            string expectedTypeName)
        {
            // arrange
            // act
            var typeInfo = TypeInfo2.Create(clrType);

            // assert
            Assert.Equal(expectedTypeName,
            typeInfo.CreateType(new StringType()).Visualize());
        }


        private class CustomStringList
            : CustomStringListBase
        {
        }

        private class CustomStringListBase
            : List<string>
        {
        }
    }
}
