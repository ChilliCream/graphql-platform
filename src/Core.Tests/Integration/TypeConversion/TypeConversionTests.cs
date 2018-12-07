
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

        public class Query
        {
            public Foo GetFoo(Foo foo)
            {
                return foo;
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
