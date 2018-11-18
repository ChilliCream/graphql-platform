using System;
using Xunit;

namespace HotChocolate.Utilities
{
    public class ReflectionUtilsTests
    {
        [Fact]
        public void GetTypeNameFromGenericType()
        {
            // arrange
            Type type = typeof(GenericNonNestedFoo<string>);

            // act
            NameString typeName = type.GetTypeName();

            // assert
            Assert.Equal(
                "HotChocolate.Utilities.GenericNonNestedFoo<System.String>",
                typeName);
        }

        [Fact]
        public void GetTypeNameFromType()
        {
            // arrange
            Type type = typeof(ReflectionUtilsTests);

            // act
            NameString typeName = type.GetTypeName();

            // assert
            Assert.Equal(
                "HotChocolate.Utilities.ReflectionUtilsTests",
                typeName);
        }

        [Fact]
        public void GetTypeNameFromGenericNestedType()
        {
            // arrange
            Type type = typeof(GenericNestedFoo<string>);

            // act
            NameString typeName = type.GetTypeName();

            // assert
            Assert.Equal(
                "HotChocolate.Utilities.ReflectionUtilsTests.GenericNestedFoo<System.String>",
                typeName);
        }

        [Fact]
        public void GetTypeNameFromNestedType()
        {
            // arrange
            Type type = typeof(Foo);

            // act
            NameString typeName = type.GetTypeName();

            // assert
            Assert.Equal(
                "HotChocolate.Utilities.ReflectionUtilsTests.Foo",
                typeName);
        }

        public class GenericNestedFoo<T>
        {
            public T Value { get; }
        }

        public class Foo
        {
            public string Value { get; }
        }
    }

    public class GenericNonNestedFoo<T>
    {
        public T Value { get; }
    }
}
