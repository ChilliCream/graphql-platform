using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class ScalarTypeTestBase
    {
        protected ISchema BuildSchema<TType>()
            where TType : ScalarType
        {
            return SchemaBuilder
                .New()
                .AddQueryType(x => x.Name("Query").Field("scalar").Type<TType>().Resolve(""))
                .Create();
        }

        protected ScalarType CreateType<TType>()
            where TType : ScalarType
        {
            return BuildSchema<TType>().GetType<ObjectType>("Query").Fields["scalar"].Type as
                ScalarType ?? throw new InvalidOperationException();
        }

        protected IValueNode CreateValueNode(Type type, object value)
        {
            switch (type.Name)
            {
                case nameof(BooleanValueNode) when value is bool b:
                    return new BooleanValueNode(b);
                case nameof(EnumValueNode):
                    return new EnumValueNode(value);
                case nameof(FloatValueNode) when value is double d:
                    return new FloatValueNode(d);
                case nameof(FloatValueNode) when value is decimal d:
                    return new FloatValueNode(d);
                case nameof(IntValueNode) when value is int i:
                    return new IntValueNode(i);
                case nameof(IntValueNode) when value is uint i:
                    return new IntValueNode(i);
                case nameof(IntValueNode) when value is ulong i:
                    return new IntValueNode(i);
                case nameof(NullValueNode):
                    return NullValueNode.Default;
                case nameof(StringValueNode) when value is string s:
                    return new StringValueNode(s);
                default:
                    throw new InvalidOperationException();
            }
        }

        protected void ExpectIsInstanceOfTypeToMatch<TType>(
            IValueNode valueSyntax,
            bool expectedResult)
            where TType : ScalarType
        {
            // arrange
            ScalarType scalar = CreateType<TType>();

            // act
            var result = scalar.IsInstanceOfType(valueSyntax);

            // assert
            Assert.Equal(expectedResult, result);
        }

        protected void ExpectIsInstanceOfTypeToMatch<TType>(
            object? runtimeValue,
            bool expectedResult)
            where TType : ScalarType
        {
            // arrange
            ScalarType scalar = CreateType<TType>();

            // act
            var result = scalar.IsInstanceOfType(runtimeValue);

            // assert
            Assert.Equal(expectedResult, result);
        }

        protected void ExpectParseLiteralToMatch<TType>(
            IValueNode valueSyntax,
            object? expectedResult)
            where TType : ScalarType
        {
            // arrange
            ScalarType scalar = CreateType<TType>();

            // act
            object? result = scalar.ParseLiteral(valueSyntax);

            // assert
            Assert.Equal(expectedResult, result);
        }

        protected void ExpectParseLiteralToThrowSerializationException<TType>(
            IValueNode valueSyntax)
            where TType : ScalarType
        {
            // arrange
            ScalarType scalar = CreateType<TType>();

            // act
            Exception? result = Record.Exception(() => scalar.ParseLiteral(valueSyntax));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        protected void ExpectParseValueToMatchType<TType>(
            object? valueSyntax,
            Type type)
            where TType : ScalarType
        {
            // arrange
            ScalarType scalar = CreateType<TType>();

            // act
            IValueNode result = scalar.ParseValue(valueSyntax);

            // assert
            Assert.Equal(type, result.GetType());
        }

        protected void ExpectParseValueToThrowSerializationException<TType>(object? runtimeValue)
            where TType : ScalarType
        {
            // arrange
            ScalarType scalar = CreateType<TType>();

            // act
            Exception? result = Record.Exception(() => scalar.ParseValue(runtimeValue));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        protected void ExpectSerializeToMatch<TType>(
            object? runtimeValue,
            object? resultValue)
            where TType : ScalarType
        {
            // arrange
            ScalarType scalar = CreateType<TType>();

            // act
            object? result = scalar.Serialize(runtimeValue);

            // assert
            Assert.Equal(resultValue, result);
        }

        protected void ExpectDeserializeToMatch<TType>(
            object? resultValue,
            object? runtimeValue)
            where TType : ScalarType
        {
            // arrange
            ScalarType scalar = CreateType<TType>();

            // act
            object? result = scalar.Deserialize(resultValue);

            // assert
            Assert.Equal(resultValue, runtimeValue);
        }

        protected void ExpectSerializeToThrowSerializationException<TType>(object runtimeValue)
            where TType : ScalarType
        {
            // arrange
            ScalarType scalar = CreateType<TType>();

            // act
            Exception? result = Record.Exception(() => scalar.Serialize(runtimeValue));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        protected void ExpectDeserializeToThrowSerializationException<TType>(object runtimeValue)
            where TType : ScalarType
        {
            // arrange
            ScalarType scalar = CreateType<TType>();

            // act
            Exception? result = Record.Exception(() => scalar.Deserialize(runtimeValue));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        protected void ExpectParseResultToMatchType<TType>(
            object? valueSyntax,
            Type type)
            where TType : ScalarType
        {
            // arrange
            ScalarType scalar = CreateType<TType>();

            // act
            IValueNode result = scalar.ParseResult(valueSyntax);

            // assert
            Assert.Equal(type, result.GetType());
        }

        protected void ExpectParseResultToThrowSerializationException<TType>(object? runtimeValue)
            where TType : ScalarType
        {
            // arrange
            ScalarType scalar = CreateType<TType>();

            // act
            Exception? result = Record.Exception(() => scalar.ParseResult(runtimeValue));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        public enum TestEnum
        {
            Foo
        }
    }
}
