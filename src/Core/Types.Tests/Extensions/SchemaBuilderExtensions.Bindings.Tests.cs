using System;
using Xunit;
using HotChocolate.Types;

namespace HotChocolate
{
    public class SchemaBuilderExtensionsBindingsTests
    {
        [Fact]
        public void BindResolver1_Builder_IsNull_ArgumentException()
        {
            // arrange
            // act
            Action action = () =>
                SchemaBuilderExtensions.BindResolver<object>(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void BindResolver2_Builder_IsNull_ArgumentException()
        {
            // arrange
            // act
            Action action = () =>
                SchemaBuilderExtensions.BindResolver<object>(null, c => { });

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void BindResolver3_Builder_IsNull_ArgumentException()
        {
            // arrange
            // act
            Action action = () =>
                SchemaBuilderExtensions.BindResolver<object>(
                    null, BindingBehavior.Implicit, c => { });

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void BindResolver3_ExplicitBinding_ConfigNull_ArgumentException()
        {
            // arrange
            // act
            Action action = () =>
                SchemaBuilderExtensions.BindResolver<object>(
                    null, BindingBehavior.Explicit, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void BindType1_Builder_IsNull_ArgumentException()
        {
            // arrange
            // act
            Action action = () =>
                SchemaBuilderExtensions.BindComplexType<object>(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void BindType2_Builder_IsNull_ArgumentException()
        {
            // arrange
            // act
            Action action = () =>
                SchemaBuilderExtensions.BindComplexType<object>(null, c => { });

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void BindType3_Builder_IsNull_ArgumentException()
        {
            // arrange
            // act
            Action action = () =>
                SchemaBuilderExtensions.BindComplexType<object>(
                    null, BindingBehavior.Implicit, c => { });

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void BindType3_ExplicitBinding_ConfigNull_ArgumentException()
        {
            // arrange
            // act
            Action action = () =>
                SchemaBuilderExtensions.BindComplexType<object>(
                    null, BindingBehavior.Explicit, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }
    }
}

