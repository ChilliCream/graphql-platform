using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Tests;
using Xunit;

namespace HotChocolate.Execution
{
    public class ScalarExecutionErrorTests
    {
        [Fact]
        public async Task OutputType_ClrValue_CannotBeConverted()
        {
            // arrange
            var schema = Schema.Create(t =>
            {
                t.RegisterQueryType<QueryType>();
            });

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ stringToName(name: \"  \") }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task OutputType_ClrValue_CannotBeParsed()
        {
            // arrange
            var schema = Schema.Create(t =>
            {
                t.RegisterQueryType<QueryType>();
            });

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ stringToFoo(name: \"  \") }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task InputType_Literal_CannotBeParsed()
        {
            // arrange
            var schema = Schema.Create(t =>
            {
                t.RegisterQueryType<QueryType>();
            });

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ nameToString(name: \"  \") }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task InputType_Variable_CannotBeDeserialized()
        {
            // arrange
            var schema = Schema.Create(t =>
            {
                t.RegisterQueryType<QueryType>();
            });

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "query a($a: Foo) { fooToString(name: $a) }",
                new Dictionary<string, object>
                {
                    {"a", " "}
                });

            // assert
            result.MatchSnapshot();
        }

        public class Query
        {
            public string StringToName(string name) => name;

            public string NameToString(string name) => name;

            public string StringToFoo(string name) => name;

            public string FooToString(string name) => name;
        }

        public class QueryType
            : ObjectType<Query>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Query> descriptor)
            {
                descriptor.Field(t => t.StringToName(default))
                    .Argument("name", a => a.Type<StringType>())
                    .Type<NameType>();

                descriptor.Field(t => t.NameToString(default))
                    .Argument("name", a => a.Type<NameType>())
                    .Type<StringType>();

                descriptor.Field(t => t.StringToFoo(default))
                    .Argument("name", a => a.Type<StringType>())
                    .Type<FooType>();

                descriptor.Field(t => t.FooToString(default))
                    .Argument("name", a => a.Type<FooType>())
                    .Type<StringType>();
            }
        }

        public class FooType
            : ScalarType
        {
            public FooType()
                : base("Foo")
            {
            }

            public override Type RuntimeType => typeof(string);

            public override bool IsInstanceOfType(IValueNode literal)
            {
                if (literal is null)
                {
                    throw new ArgumentNullException(nameof(literal));
                }

                if (literal is NullValueNode)
                {
                    return true;
                }

                return literal is StringValueNode s && s.Value == "a";
            }

            public override bool IsInstanceOfType(object value)
            {
                if (value is null)
                {
                    return true;
                }

                return value is string s && s == "a";
            }

            public override object ParseLiteral(IValueNode literal, bool withDefaults = true)
            {
                if (literal is null)
                {
                    throw new ArgumentNullException(nameof(literal));
                }

                if (literal is NullValueNode)
                {
                    return null;
                }

                if (literal is StringValueNode s && s.Value == "a")
                {
                    return "a";
                }

                throw new SerializationException("StringValue is not a.", this);
            }

            public override IValueNode ParseValue(object value)
            {
                if (value is null)
                {
                    return NullValueNode.Default;
                }

                if (value is string s && s == "a")
                {
                    return new StringValueNode("a");
                }

                throw new SerializationException("String is not a.", this);
            }

            public override IValueNode ParseResult(object resultValue) => ParseValue(resultValue);

            public override bool TrySerialize(
                object runtimeValue, out object resultValue)
            {
                if (runtimeValue is null)
                {
                    resultValue = null;
                    return true;
                }

                if (runtimeValue is string s && s == "a")
                {
                    resultValue = new StringValueNode("a");
                    return true;
                }

                resultValue = null;
                return false;
            }

            public override bool TryDeserialize(
                object resultValue, out object runtimeValue)
            {
                if (resultValue is null)
                {
                    runtimeValue = null;
                    return true;
                }

                if (resultValue is string s && s == "a")
                {
                    runtimeValue = "a";
                    return true;
                }

                runtimeValue = null;
                return false;
            }
        }
    }
}
