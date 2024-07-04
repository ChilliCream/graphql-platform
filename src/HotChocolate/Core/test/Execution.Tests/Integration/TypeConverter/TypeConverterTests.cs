using HotChocolate.Tests;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Execution.Integration.TypeConverter;

public class TypeConverterTests
{
    [Fact]
    public async Task VariablesAreCoercedToTypesOtherThanTheDefinedClrTypes()
    {
        Snapshot.FullName();
        await ExpectValid(
                @"
                query foo($a: FooInput) {
                    foo(foo: $a) {
                        id
                        time
                        number
                    }
                }",
                request: r => r.SetVariableValues(
                    new Dictionary<string, object>
                    {
                        {
                            "a",
                            new Dictionary<string, object>
                            {
                                { "id", "934b987bc0d842bbabfd8a3b3f8b476e" },
                                { "time", "2018-05-29T01:00Z" },
                                { "number", (byte)123 },
                            }
                        }
                    }),
                configure: c => c.AddQueryType<Query>())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task VariableIsCoercedToTypesOtherThanTheDefinedClrTypes()
    {
        Snapshot.FullName();
        await ExpectValid(
                @"
                query foo($time: DateTime) {
                    time(time: $time)
                }",
                request: r => r.SetVariableValues(new Dictionary<string, object> { { "time", "2018-05-29T01:00Z" }, }),
                configure: c => c.AddQueryType<QueryType>())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task VariableIsNotSerializedAndMustBeConvertedToClrType()
    {
        Snapshot.FullName();
        var time = new DateTime(2018, 01, 01, 12, 10, 10, DateTimeKind.Utc);
        await ExpectValid(
                @"
                query foo($time: DateTime) {
                    time(time: $time)
                }",
                request: r => r.SetVariableValues(new Dictionary<string, object> { { "time", time }, }),
                configure: c => c.AddQueryType<QueryType>())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task VariableIsPartlyNotSerializedAndMustBeConvertedToClrType()
    {
        Snapshot.FullName();
        await ExpectValid(
                @"
                query foo($a: FooInput) {
                    foo(foo: $a) {
                        id
                        time
                        number
                    }
                }",
                request: r => r.SetVariableValues(
                    new Dictionary<string, object>
                    {
                        {
                            "a",
                            new Dictionary<string, object>
                            {
                                { "id", "934b987bc0d842bbabfd8a3b3f8b476e" },
                                { "time", "2018-05-29T01:00Z" },
                                { "number", (byte)123 },
                            }
                        },
                    }),
                configure: c => c.AddQueryType<QueryType>())
            .MatchSnapshotAsync();
    }

    [Fact]
    public void Register_TypeConverter_As_Service()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        services.AddGraphQL().AddTypeConverter<IntToStringConverter>();

        // assert
        var conversion =
            services.BuildServiceProvider().GetService<ITypeConverter>();
        Assert.Equal("123", conversion.Convert<int, string>(123));
    }

    [Fact]
    public void Register_DelegateTypeConverter_As_Service()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        services.AddGraphQL().AddTypeConverter<int, string>(
            from => from.ToString() + "_123");

        // assert
        var conversion =
            services.BuildServiceProvider().GetService<ITypeConverter>();
        Assert.Equal("123_123", conversion.Convert<int, string>(123));
    }

    [Fact]
    public void Register_Multiple_TypeConverters_As_Service()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddGraphQLCore();

        // act
        services.AddTypeConverter<int, string>(
            from => from.ToString() + "_123");
        services.AddTypeConverter<char, string>(
            from => from + "_123");

        // assert
        var conversion =
            services.BuildServiceProvider().GetService<ITypeConverter>();
        Assert.Equal("123_123", conversion.Convert<int, string>(123));
        Assert.Equal("a_123", conversion.Convert<char, string>('a'));
    }

    [Fact]
    public void Convert_Null_To_Value_Type_Default()
    {
        var empty = TypeConverterExtensions.Convert<object, Guid>(
            DefaultTypeConverter.Default,
            null);
        Assert.Equal(Guid.Empty, empty);
    }

    [Fact]
    public async Task UseSet_As_InputType()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<QuerySet>()
                .ExecuteRequestAsync("{ set(set: [\"abc\", \"abc\"]) }");

        CookieCrumble.SnapshotExtensions.MatchInlineSnapshot(
            result,
            """
            {
              "data": {
                "set": [
                  "abc"
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task UseHashSet_As_InputType()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<QuerySet>()
                .ExecuteRequestAsync("{ set2(set: [\"abc\", \"abc\"]) }");

        CookieCrumble.SnapshotExtensions.MatchInlineSnapshot(
            result,
            """
            {
              "data": {
                "set2": [
                  "abc"
                ]
              }
            }
            """);
    }

    public class QuerySet
    {
        public ISet<string> Set(ISet<string> set) => set;

        public HashSet<string> Set2(HashSet<string> set) => set;
    }

    public class QueryType : ObjectType<Query>
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

        [GraphQLType(typeof(DateTimeType))] public DateTime? Time { get; set; }

        [GraphQLType(typeof(LongType))] public int? Number { get; set; }
    }

    public class IntToStringConverter : IChangeTypeProvider
    {
        public bool TryCreateConverter(
            Type source,
            Type target,
            ChangeTypeProvider root,
            out ChangeType converter)
        {
            if (source == typeof(int) && target == typeof(string))
            {
                converter = input => input?.ToString();
                return true;
            }

            converter = null;
            return false;
        }
    }
}