
using System;
using System.Collections.Generic;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Integration.TypeConversion
{
    public class TypeConversionTests
    {
        [Fact]
        public void VariablesAreCoercedToTypesOtherThanTheDefinedClrTypes()
        {
            // arrange
            ISchema schema = Schema.Create(c => c.RegisterQueryType<Query>());
            var variables = new Dictionary<string, object>
            {
                {
                    "a",
                    new Dictionary<string, object>
                    {
                        {"id", "934b987bc0d842bbabfd8a3b3f8b476e"},
                        {"time", "2018-05-29T01:00Z"},
                        {"number", "123"}
                    }
                }
            };

            // act
            IExecutionResult result = schema.Execute(@"
                query foo($a: FooInput) {
                    foo(foo: $a) {
                        id
                        time
                        number
                    }
                }", variables);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void VariableIsCoercedToTypesOtherThanTheDefinedClrTypes()
        {
            // arrange
            ISchema schema = Schema.Create(
                c => c.RegisterQueryType<QueryType>());

            var variables = new Dictionary<string, object>
            {
                {"time", "2018-05-29T01:00Z"}
            };

            // act
            IExecutionResult result = schema.Execute(@"
                query foo($time: DateTime) {
                    time(time: $time)
                }", variables);

            // assert
            result.Snapshot();
        }

        [Fact]
        public void VariableIsNotSerializedAndMustBeConvertedToClrType()
        {
            // arrange
            ISchema schema = Schema.Create(
                c => c.RegisterQueryType<QueryType>());

            var time = new DateTime(2018, 01, 01, 12, 10, 10, DateTimeKind.Utc);

            var variables = new Dictionary<string, object>
            {
                {"time", time}
            };

            // act
            IExecutionResult result = schema.Execute(@"
                query foo($time: DateTime) {
                    time(time: $time)
                }", variables);

            // assert
            result.Snapshot();
        }

        public class QueryType
            : ObjectType<Query>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Query> descriptor)
            {
                descriptor.Field(t => t.GetTime(default))
                    .Argument("time", a => a.Type<DateTimeType>())
                    .Type<DateTimeType>();
            }
        }

        public class Query
        {
            public Foo GetFoo(Foo foo)
            {
                return foo;
            }

            public DateTime? GetTime(DateTime? time)
            {
                return time;
            }
        }

        public class Foo
        {
            [GraphQLType(typeof(NonNullType<IdType>))]
            public Guid Id { get; set; }

            [GraphQLType(typeof(DateTimeType))]
            public DateTime? Time { get; set; }

            [GraphQLType(typeof(LongType))]
            public int? Number { get; set; }
        }
    }
}
