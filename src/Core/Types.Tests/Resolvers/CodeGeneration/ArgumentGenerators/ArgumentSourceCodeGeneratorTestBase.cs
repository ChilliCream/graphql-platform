using System;
using ChilliCream.Testing;
using HotChocolate.Resolvers.CodeGeneration;
using Xunit;

namespace HotChocolate.Resolvers
{
    public abstract class ArgumentSourceCodeGeneratorTestBase
    {
        private readonly ArgumentSourceCodeGenerator _generator;
        private readonly ArgumentKind _validArgumentKind;
        private readonly ArgumentKind _invalidArgumentKind;
        private readonly Type _argumentType;

        internal ArgumentSourceCodeGeneratorTestBase(
            ArgumentSourceCodeGenerator generator,
            Type argumentType,
            ArgumentKind validArgumentKind,
            ArgumentKind invalidArgumentKind)
        {
            _generator = generator
                ?? throw new ArgumentNullException(nameof(generator));
            _argumentType = argumentType
                ?? throw new ArgumentNullException(nameof(argumentType));
            _validArgumentKind = validArgumentKind;
            _invalidArgumentKind = invalidArgumentKind;
        }

        [Fact]
        public void Generate_ArgumentDescriptor_ArgumentInvocationSourceCode()
        {
            // arrange
            var argumentDescriptor = new ArgumentDescriptor(
                "foo", "bar",
                _validArgumentKind,
                _argumentType);

            // act
            string result = _generator.Generate("foo", argumentDescriptor);

            // assert
            result.Snapshot($"Generate_{_validArgumentKind}_ArgumentInvocationSourceCode");
        }

        [Fact]
        public void CanHandle_ValidArgumentDescriptor_True()
        {
            // arrange
            var argumentDescriptor = new ArgumentDescriptor(
                "foo", "bar",
                _validArgumentKind,
                _argumentType);

            // act
            bool result = _generator.CanHandle(argumentDescriptor);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void CanHandle_InvalidArgumentDescriptor_False()
        {
            // arrange
            var argumentDescriptor = new ArgumentDescriptor(
                "foo", "bar",
                _invalidArgumentKind,
                typeof(object));

            // act
            bool result = _generator.CanHandle(argumentDescriptor);

            // assert
            Assert.False(result);
        }
    }
}
