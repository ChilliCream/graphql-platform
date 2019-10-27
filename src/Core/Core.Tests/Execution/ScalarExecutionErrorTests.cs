using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Types;
using Snapshooter.Xunit;
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

            IQueryExecutor executor = schema.MakeExecutable();

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

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ stringToFoo(name: \"  \") }");

            // assert
            result.MatchSnapshot(options =>
                options.IgnoreField("Errors[0].Exception"));
        }

        [Fact]
        public async Task InputType_Literal_CannotBeParsed()
        {
            // arrange
            var schema = Schema.Create(t =>
            {
                t.RegisterQueryType<QueryType>();
            });

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ nameToString(name: \"  \") }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task InputType_Variable_CannotBeDeserialized()
        {
            // arrange
            var schema = Schema.Create(t =>
            {
                t.RegisterQueryType<QueryType>();
            });

            IQueryExecutor executor = schema.MakeExecutable();

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

            public override Type ClrType => typeof(string);

            public override bool IsInstanceOfType(IValueNode literal)
            {
                if (literal == null)
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

            public override object ParseLiteral(IValueNode literal)
            {
                if (literal == null)
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

                throw new ScalarSerializationException(
                    "StringValue is not a.");
            }

            public override IValueNode ParseValue(object value)
            {
                if (value == null)
                {
                    return NullValueNode.Default;
                }

                if (value is string s && s == "a")
                {
                    return new StringValueNode("a");
                }

                throw new ScalarSerializationException(
                    "String is not a.");
            }

            public override bool TrySerialize(
                object value, out object serialized)
            {
                if (value == null)
                {
                    serialized = null;
                    return true;
                }

                if (value is string s && s == "a")
                {
                    serialized = new StringValueNode("a");
                    return true;
                }

                serialized = null;
                return false;
            }

            public override bool TryDeserialize(
                object serialized, out object value)
            {
                if (serialized == null)
                {
                    value = null;
                    return true;
                }

                if (serialized is string s && s == "a")
                {
                    value = "a";
                    return true;
                }

                value = null;
                return false;
            }
        }
    }
}
