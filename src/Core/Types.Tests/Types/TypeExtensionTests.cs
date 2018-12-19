using System.Linq;
using HotChocolate.Configuration;
using Xunit;

namespace HotChocolate.Types
{
    public class TypeExtensionTests
    {
        [Fact]
        public void IsEquals_TwoStringNonNullTypes_True()
        {
            // arrange
            var x = new NonNullType(new StringType());
            var y = new NonNullType(new StringType());

            // act
            bool result = x.IsEqualTo(y);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsEquals_TwoStringListTypes_True()
        {
            // arrange
            var x = new ListType(new StringType());
            var y = new ListType(new StringType());

            // act
            bool result = x.IsEqualTo(y);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsEquals_TwoStringNonNullListTypes_True()
        {
            // arrange
            var x = new NonNullType(new ListType(new StringType()));
            var y = new NonNullType(new ListType(new StringType()));

            // act
            bool result = x.IsEqualTo(y);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsEquals_NonNullStringListToStringList_False()
        {
            // arrange
            var x = new NonNullType(new ListType(new StringType()));
            var y = new ListType(new StringType());

            // act
            bool result = x.IsEqualTo(y);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsEquals_StringToSelf_True()
        {
            // arrange
            var x = new StringType();

            // act
            bool result = x.IsEqualTo(x);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsEquals_StringListToIntList_False()
        {
            // arrange
            var x = new ListType(new StringType());
            var y = new ListType(new IntType());

            // act
            bool result = x.IsEqualTo(y);

            // assert
            Assert.False(result);
        }
    }
}
