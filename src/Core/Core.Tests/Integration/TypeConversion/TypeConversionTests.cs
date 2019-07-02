using System.ComponentModel;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;
using HotChocolate.Utilities;

namespace HotChocolate.Integration.TypeConversion
{
    public class TypeConversionTests
    {
        [Fact]
        public async Task VariablesAreCoercedToTypesOtherThanTheDefinedClrTypes()
        {
            // arrange
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterQueryType<Query>();
                c.RegisterExtendedScalarTypes();
            });
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
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(@"
                    query foo($a: FooInput) {
                        foo(foo: $a) {
                            id
                            time
                            number
                        }
                    }", variables);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task VariableIsCoercedToTypesOtherThanTheDefinedClrTypes()
        {
            // arrange
            ISchema schema = Schema.Create(
                c => c.RegisterQueryType<QueryType>());

            var variables = new Dictionary<string, object>
            {
                {"time", "2018-05-29T01:00Z"}
            };

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(@"
                    query foo($time: DateTime) {
                        time(time: $time)
                    }", variables);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task VariableIsNotSerializedAndMustBeConvertedToClrType()
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
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(@"
                    query foo($time: DateTime) {
                        time(time: $time)
                    }", variables);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task VariableIsPartlyNotSerializedAndMustBeConvertedToClrType()
        {
            // arrange
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterQueryType<Query>();
                c.RegisterExtendedScalarTypes();
            });

            var variables = new Dictionary<string, object>
            {
                {
                    "a",
                    new Dictionary<string, object>
                    {
                        {"id", "934b987bc0d842bbabfd8a3b3f8b476e"},
                        {"time", "2018-05-29T01:00Z"},
                        {"number", 123}
                    }
                }
            };

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(@"
                    query foo($a: FooInput) {
                        foo(foo: $a) {
                            id
                            time
                            number
                        }
                    }", variables);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Register_TypeConverter_As_Service()
        {
            // arrange
            var service = new ServiceCollection();

            // act
            service.AddTypeConverter<IntToStringConverter>();

            // assert
            ITypeConversion conversion =
                service.BuildServiceProvider().GetService<ITypeConversion>();
            Assert.Equal("123_123", conversion.Convert<int, string>(123));
        }

        [Fact]
        public void Register_DelegateTypeConverter_As_Service()
        {
            // arrange
            var service = new ServiceCollection();

            // act
            service.AddTypeConverter<int, string>(
                from => from.ToString() + "_123");

            // assert
            ITypeConversion conversion =
                service.BuildServiceProvider().GetService<ITypeConversion>();
            Assert.Equal("123_123", conversion.Convert<int, string>(123));
        }

        [Fact]
        public void Register_Multiple_TypeConverters_As_Service()
        {
            // arrange
            var service = new ServiceCollection();

            // act
            service.AddTypeConverter<int, string>(
                from => from.ToString() + "_123");
            service.AddTypeConverter<char, string>(
                from => from + "_123");

            // assert
            ITypeConversion conversion =
                service.BuildServiceProvider().GetService<ITypeConversion>();
            Assert.Equal("123_123", conversion.Convert<int, string>(123));
            Assert.Equal("a_123", conversion.Convert<char, string>('a'));
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

        public class IntToStringConverter
            : TypeConverter<int, string>
        {
            public override string Convert(int from)
            {
                return from.ToString() + "_123";
            }
        }
    }
}
