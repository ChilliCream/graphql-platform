using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Composite;

public static class AuthorizationDirectiveTests
{
    [Fact]
    public static async Task Authenticated_Should_RenderOnEveryLocation_When_AppliedByAttribute()
    {
        // arrange
        var builder =
            new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<AuthTypes.Query>()
                .AddType<AuthTypes.ThingType>()
                .AddType<AuthTypes.Impl>()
                .AddType<AuthTypes.CustomType>();

        // act
        var schema = await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchMarkdownSnapshot();
    }

    [Fact]
    public static async Task Authenticated_Should_RenderOnEveryLocation_When_AppliedByDescriptor()
    {
        // arrange
        var builder =
            new ServiceCollection()
                .AddGraphQL()
                .UseField(x => x)
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("field").Type<StringType>().Authenticated();
                    d.Field("thing").Type("Thing");
                    d.Field("paint").Type("Paint");
                    d.Field("custom").Type("Custom");
                })
                .AddType(new InterfaceType(d =>
                {
                    d.Name("Thing");
                    d.Field("name").Type<StringType>().Authenticated();
                    d.Authenticated();
                }))
                .AddType(new ObjectType(d =>
                {
                    d.Name("Impl");
                    d.Implements("Thing");
                    d.Field("name").Type<StringType>().Authenticated();
                    d.Authenticated();
                }))
                .AddType(new EnumType(d =>
                {
                    d.Name("Paint");
                    d.Value("RED");
                    d.Authenticated();
                }))
                .AddType(new ConfigurableScalarType("Custom", d => d.Authenticated()));

        // act
        var schema = await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchMarkdownSnapshot();
    }

    [Fact]
    public static async Task RequiresScopes_Should_RenderOnEveryLocation_When_AppliedByAttribute()
    {
        // arrange
        var builder =
            new ServiceCollection()
                .AddGraphQL()
                .UseField(x => x)
                .AddQueryType<ScopesTypes.Query>()
                .AddType<ScopesTypes.ThingType>()
                .AddType<ScopesTypes.Impl>()
                .AddType<ScopesTypes.CustomType>();

        // act
        var schema = await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchMarkdownSnapshot();
    }

    [Fact]
    public static async Task RequiresScopes_Should_RenderOnEveryLocation_When_AppliedByDescriptor()
    {
        // arrange
        var builder =
            new ServiceCollection()
                .AddGraphQL()
                .UseField(x => x)
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("field").Type<StringType>().RequiresScopes("write", "read").RequiresScopes("admin");
                    d.Field("thing").Type("Thing");
                    d.Field("paint").Type("Paint");
                    d.Field("custom").Type("Custom");
                })
                .AddType(new InterfaceType(d =>
                {
                    d.Name("Thing");
                    d.Field("name").Type<StringType>().RequiresScopes("write", "read").RequiresScopes("admin");
                    d.RequiresScopes("write", "read").RequiresScopes("admin");
                }))
                .AddType(new ObjectType(d =>
                {
                    d.Name("Impl");
                    d.Implements("Thing");
                    d.Field("name").Type<StringType>().RequiresScopes("write", "read").RequiresScopes("admin");
                    d.RequiresScopes("write", "read").RequiresScopes("admin");
                }))
                .AddType(new EnumType(d =>
                {
                    d.Name("Paint");
                    d.Value("RED");
                    d.RequiresScopes("write", "read").RequiresScopes("admin");
                }))
                .AddType(new ConfigurableScalarType("Custom", d => d.RequiresScopes("write", "read").RequiresScopes("admin")));

        // act
        var schema = await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchMarkdownSnapshot();
    }

    [Fact]
    public static async Task Policy_Should_RenderOnEveryLocation_When_AppliedByAttribute()
    {
        // arrange
        var builder =
            new ServiceCollection()
                .AddGraphQL()
                .UseField(x => x)
                .AddQueryType<PolTypes.Query>()
                .AddType<PolTypes.ThingType>()
                .AddType<PolTypes.Impl>()
                .AddType<PolTypes.CustomType>();

        // act
        var schema = await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchMarkdownSnapshot();
    }

    [Fact]
    public static async Task Policy_Should_RenderOnEveryLocation_When_AppliedByDescriptor()
    {
        // arrange
        var builder =
            new ServiceCollection()
                .AddGraphQL()
                .UseField(x => x)
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("field").Type<StringType>().Policy("p2", "p1").Policy("p3");
                    d.Field("thing").Type("Thing");
                    d.Field("paint").Type("Paint");
                    d.Field("custom").Type("Custom");
                })
                .AddType(new InterfaceType(d =>
                {
                    d.Name("Thing");
                    d.Field("name").Type<StringType>().Policy("p2", "p1").Policy("p3");
                    d.Policy("p2", "p1").Policy("p3");
                }))
                .AddType(new ObjectType(d =>
                {
                    d.Name("Impl");
                    d.Implements("Thing");
                    d.Field("name").Type<StringType>().Policy("p2", "p1").Policy("p3");
                    d.Policy("p2", "p1").Policy("p3");
                }))
                .AddType(new EnumType(d =>
                {
                    d.Name("Paint");
                    d.Value("RED");
                    d.Policy("p2", "p1").Policy("p3");
                }))
                .AddType(new ConfigurableScalarType("Custom", d => d.Policy("p2", "p1").Policy("p3")));

        // act
        var schema = await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchMarkdownSnapshot();
    }

    [Fact]
    public static async Task RequiresScopes_Should_RenderCanonicalOrder_When_GroupsAreUnsortedAndDuplicated()
    {
        // arrange
        var builder =
            new ServiceCollection()
                .AddGraphQL()
                .UseField(x => x)
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("field")
                        .Type<StringType>()
                        .RequiresScopes("b", "a", "a")
                        .RequiresScopes("c")
                        .RequiresScopes("a", "b");
                });

        // act
        var schema = await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchMarkdownSnapshot();
    }

    [Fact]
    public static async Task Policy_Should_RenderCanonicalOrder_When_GroupsAreUnsortedAndDuplicated()
    {
        // arrange
        var builder =
            new ServiceCollection()
                .AddGraphQL()
                .UseField(x => x)
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("field")
                        .Type<StringType>()
                        .Policy("b", "a", "a")
                        .Policy("c")
                        .Policy("a", "b");
                });

        // act
        var schema = await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchMarkdownSnapshot();
    }

    [Fact]
    public static async Task RequiresScopes_Should_Throw_When_GroupIsEmpty()
    {
        // arrange
        var builder =
            new ServiceCollection()
                .AddGraphQL()
                .UseField(x => x)
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("field").Type<StringType>().RequiresScopes();
                });

        // act
        async Task Act() => await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var exception = await Assert.ThrowsAsync<SchemaException>(Act);
        exception.Errors[0].Message.MatchInlineSnapshot(
            "The @requiresScopes directive requires at least one value per group. (Parameter 'scopes')");
    }

    [Fact]
    public static async Task Policy_Should_Throw_When_PolicyNameIsEmpty()
    {
        // arrange
        var builder =
            new ServiceCollection()
                .AddGraphQL()
                .UseField(x => x)
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("field").Type<StringType>().Policy("");
                });

        // act
        async Task Act() => await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var exception = await Assert.ThrowsAsync<SchemaException>(Act);
        exception.Errors[0].Message.MatchInlineSnapshot(
            "The @policy directive does not allow null or empty values. (Parameter 'policies')");
    }

    private sealed class ConfigurableScalarType : StringType
    {
        private readonly Action<IScalarTypeDescriptor> _configure;

        public ConfigurableScalarType(string name, Action<IScalarTypeDescriptor> configure)
            : base(name)
        {
            _configure = configure;
        }

        protected override void Configure(IScalarTypeDescriptor descriptor)
            => _configure(descriptor);
    }

    private static class AuthTypes
    {
        public class Query
        {
            [Authenticated]
            public string? Field() => null;

            public IThing? Thing() => null;

            public Paint? Paint() => null;

            [GraphQLType<CustomType>]
            public string? Custom() => null;
        }

        [Authenticated]
        [InterfaceType("Thing")]
        public interface IThing
        {
            [Authenticated]
            string? Name { get; }
        }

        public class ThingType : InterfaceType<IThing>;

        [Authenticated]
        public class Impl : IThing
        {
            [Authenticated]
            public string? Name => null;
        }

        [Authenticated]
        public enum Paint
        {
            Red
        }

        [Authenticated]
        public class CustomType : StringType
        {
            public CustomType()
                : base("Custom")
            {
            }
        }
    }

    private static class ScopesTypes
    {
        public class Query
        {
            [RequiresScopes("write", "read")]
            [RequiresScopes("admin")]
            public string? Field() => null;

            public IThing? Thing() => null;

            public Paint? Paint() => null;

            [GraphQLType<CustomType>]
            public string? Custom() => null;
        }

        [RequiresScopes("write", "read")]
        [RequiresScopes("admin")]
        [InterfaceType("Thing")]
        public interface IThing
        {
            [RequiresScopes("write", "read")]
            [RequiresScopes("admin")]
            string? Name { get; }
        }

        public class ThingType : InterfaceType<IThing>;

        [RequiresScopes("write", "read")]
        [RequiresScopes("admin")]
        public class Impl : IThing
        {
            [RequiresScopes("write", "read")]
            [RequiresScopes("admin")]
            public string? Name => null;
        }

        [RequiresScopes("write", "read")]
        [RequiresScopes("admin")]
        public enum Paint
        {
            Red
        }

        [RequiresScopes("write", "read")]
        [RequiresScopes("admin")]
        public class CustomType : StringType
        {
            public CustomType()
                : base("Custom")
            {
            }
        }
    }

    private static class PolTypes
    {
        public class Query
        {
            [Policy("p2", "p1")]
            [Policy("p3")]
            public string? Field() => null;

            public IThing? Thing() => null;

            public Paint? Paint() => null;

            [GraphQLType<CustomType>]
            public string? Custom() => null;
        }

        [Policy("p2", "p1")]
        [Policy("p3")]
        [InterfaceType("Thing")]
        public interface IThing
        {
            [Policy("p2", "p1")]
            [Policy("p3")]
            string? Name { get; }
        }

        public class ThingType : InterfaceType<IThing>;

        [Policy("p2", "p1")]
        [Policy("p3")]
        public class Impl : IThing
        {
            [Policy("p2", "p1")]
            [Policy("p3")]
            public string? Name => null;
        }

        [Policy("p2", "p1")]
        [Policy("p3")]
        public enum Paint
        {
            Red
        }

        [Policy("p2", "p1")]
        [Policy("p3")]
        public class CustomType : StringType
        {
            public CustomType()
                : base("Custom")
            {
            }
        }
    }
}
