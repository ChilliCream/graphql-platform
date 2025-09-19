using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using Moq;

namespace HotChocolate.Types;

public class FromJsonDescriptorTests
{
    [Fact]
    public async Task MapField()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddObjectType(
                    d =>
                    {
                        d.Name("Foo");
                        d.Field("bar").Type<StringType>().FromJson();
                    })
                .AddJsonSupport()
                .ExecuteRequestAsync("{ foo { bar } }");

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "foo": {
                  "bar": "abc"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task MapField_With_Name()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddObjectType(
                    d =>
                    {
                        d.Name("Foo");
                        d.Field("baz").Type<StringType>().FromJson("bar");
                    })
                .AddJsonSupport()
                .ExecuteRequestAsync("{ foo { baz } }");

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "foo": {
                  "baz": "abc"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task MapField_Explicitly()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddObjectType(
                    d =>
                    {
                        d.Name("Foo");
                        d.Field("baz").Type<StringType>()
                            .FromJson(t => t.GetProperty("bar").GetString());
                    })
                .AddJsonSupport()
                .ExecuteRequestAsync("{ foo { baz } }");

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "foo": {
                  "baz": "abc"
                }
              }
            }
            """);
    }

    [Fact]
    public void FromJson_1_Descriptor_Is_Null()
    {
        void Fail() => JsonObjectTypeExtensions.FromJson(null!);
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void FromJson_2_Descriptor_Is_Null()
    {
        void Fail() => JsonObjectTypeExtensions.FromJson(null!, element => "");
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void FromJson_2_Resolver_Is_Null()
    {
        var mock = new Mock<IObjectFieldDescriptor>();
        void Fail() => JsonObjectTypeExtensions.FromJson(
            mock.Object,
            default(Func<JsonElement, string>)!);
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void AddJsonSupport_Builder_Is_Null()
    {
        void Fail() => JsonRequestExecutorBuilderExtensions.AddJsonSupport(null!);
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public async Task MapDataTimeField_ValueIsNull_NullIsReturned()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryNullProp>()
                .AddObjectType(
                    d =>
                    {
                        d.Name("Foo");
                        d.Field("baz").Type<DateTimeType>().FromJson("bar");
                    })
                .AddJsonSupport()
                .ExecuteRequestAsync("{ foo { baz } }");

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "foo": {
                  "baz": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task MapDataField_ValueIsNull_NullIsReturned()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryNullProp>()
                .AddObjectType(
                    d =>
                    {
                        d.Name("Foo");
                        d.Field("baz").Type<DateType>().FromJson("bar");
                    })
                .AddJsonSupport()
                .ExecuteRequestAsync("{ foo { baz } }");

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "foo": {
                  "baz": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task MapLong_ValueIsNull_NullIsReturned()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryNullProp>()
                .AddObjectType(
                    d =>
                    {
                        d.Name("Foo");
                        d.Field("baz").Type<LongType>().FromJson("bar");
                    })
                .AddJsonSupport()
                .ExecuteRequestAsync("{ foo { baz } }");

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "foo": {
                  "baz": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task MapInt_ValueIsNull_NullIsReturned()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryNullProp>()
                .AddObjectType(
                    d =>
                    {
                        d.Name("Foo");
                        d.Field("baz").Type<IntType>().FromJson("bar");
                    })
                .AddJsonSupport()
                .ExecuteRequestAsync("{ foo { baz } }");

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "foo": {
                  "baz": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task MapShort_ValueIsNull_NullIsReturned()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryNullProp>()
                .AddObjectType(
                    d =>
                    {
                        d.Name("Foo");
                        d.Field("baz").Type<ShortType>().FromJson("bar");
                    })
                .AddJsonSupport()
                .ExecuteRequestAsync("{ foo { baz } }");

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "foo": {
                  "baz": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task MapUrl_ValueIsNull_NullIsReturned()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryNullProp>()
                .AddObjectType(
                    d =>
                    {
                        d.Name("Foo");
                        d.Field("baz").Type<UrlType>().FromJson("bar");
                    })
                .AddJsonSupport()
                .ExecuteRequestAsync("{ foo { baz } }");

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "foo": {
                  "baz": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task MapUuid_ValueIsNull_NullIsReturned()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryNullProp>()
                .AddObjectType(
                    d =>
                    {
                        d.Name("Foo");
                        d.Field("baz").Type<UuidType>().FromJson("bar");
                    })
                .AddJsonSupport()
                .ExecuteRequestAsync("{ foo { baz } }");

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "foo": {
                  "baz": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task MapFloat_ValueIsNull_NullIsReturned()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryNullProp>()
                .AddObjectType(
                    d =>
                    {
                        d.Name("Foo");
                        d.Field("baz").Type<FloatType>().FromJson("bar");
                    })
                .AddJsonSupport()
                .ExecuteRequestAsync("{ foo { baz } }");

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "foo": {
                  "baz": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task MapDecimal_ValueIsNull_NullIsReturned()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryNullProp>()
                .AddObjectType(
                    d =>
                    {
                        d.Name("Foo");
                        d.Field("baz").Type<DecimalType>().FromJson("bar");
                    })
                .AddJsonSupport()
                .ExecuteRequestAsync("{ foo { baz } }");

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "foo": {
                  "baz": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task MapBoolean_ValueIsNull_NullIsReturned()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryNullProp>()
                .AddObjectType(
                    d =>
                    {
                        d.Name("Foo");
                        d.Field("baz").Type<BooleanType>().FromJson("bar");
                    })
                .AddJsonSupport()
                .ExecuteRequestAsync("{ foo { baz } }");

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "foo": {
                  "baz": null
                }
              }
            }
            """);
    }

    public class Query
    {
        [GraphQLType("Foo")]
        public JsonElement GetFoo() => JsonDocument.Parse(@"{ ""bar"": ""abc"" }").RootElement;
    }

    public class QueryNullProp
    {
        [GraphQLType("Foo")]
        public JsonElement GetFoo() => JsonDocument.Parse(@"{ ""bar"": null }").RootElement;
    }

    public class FooType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Foo");
            descriptor.Field("bar").Type<StringType>().FromJson();
        }
    }

    public class FooTypeWithExplicitMapping : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Foo");
            descriptor.Field("baz").Type<StringType>().FromJson("bar");
        }
    }
}
