using System.Reflection;
using CookieCrumble;
using GreenDonut;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class BatchResolverSchemaValidationTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Schema_Should_ReportUnsupportedMiddleware_When_BatchFieldHasNoCounterpart(
        bool attribute,
        bool validateOrder)
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQL()
            .ModifyOptions(o => o.ValidatePipelineOrder = validateOrder)
            .AddDirectiveType(new DirectiveType(d => d.Name("wrap")
                .Location(DirectiveLocation.FieldDefinition)
                .Use((next, _) => context => next(context))))
            .AddQueryType(d =>
            {
                if (attribute)
                {
                    d.Field<UnsupportedResolvers>(r => r.GetUnkeyed());
                    d.Field<UnsupportedResolvers>(r => r.GetKeyed());
                    d.Field<UnsupportedResolvers>(r => r.GetDirective());
                }
                else
                {
                    Batch(d.Field("unkeyed")).Use(next => next);
                    Batch(d.Field("keyed")).Extend().Configuration.MiddlewareConfigurations
                        .Add(new(next => next, key: "custom"));
                    Batch(d.Field("directive")).Directive("wrap");
                }
            });

        // act
        var exception = await Assert.ThrowsAsync<SchemaException>(async () =>
            await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken));

        // assert
        exception.Errors.Select(BatchSchemaErrorSnapshot.Create)
            .MatchSnapshot(postFix: attribute.ToString());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Schema_Should_ReportMutationBatchResolver_When_OrderValidationIsOptional(
        bool attribute,
        bool validateOrder)
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQL()
            .ModifyOptions(o => o.ValidatePipelineOrder = validateOrder)
            .AddQueryType(d => d.Field("noop").Resolve("noop"))
            .AddMutationType(d =>
            {
                if (attribute)
                {
                    d.Field<UnsupportedResolvers>(r => r.GetValue());
                }
                else
                {
                    Batch(d.Field("value"));
                }
            });

        // act
        var exception = await Assert.ThrowsAsync<SchemaException>(async () =>
            await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken));

        // assert
        exception.Errors.Select(BatchSchemaErrorSnapshot.Create)
            .MatchSnapshot(postFix: attribute.ToString());
    }

    [Theory]
    [InlineData(null, null, false)]
    [InlineData("custom", "CUSTOM", false)]
    [InlineData("custom", "other", false)]
    [InlineData("custom", "custom", true)]
    public async Task Schema_Should_MatchOnlyNonNullOrdinalKeys_When_PairingMiddleware(
        string? regularKey,
        string? batchKey,
        bool supported)
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQL().AddQueryType(d =>
        {
            var field = Batch(d.Field("value")).Extend().Configuration;
            field.MiddlewareConfigurations.Add(new(next => next, key: regularKey));
            field.MiddlewareConfigurations.Add(new(next => next, key: regularKey));
            field.BatchMiddlewareConfigurations.Add(new(next => next, key: batchKey));
        });

        // act
        if (supported)
        {
            var schema = await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

            // assert
            schema.ToString().MatchInlineSnapshot("""
                schema {
                  query: Query
                }

                type Query {
                  value: String
                }
                """);
        }
        else
        {
            var exception = await Assert.ThrowsAsync<SchemaException>(async () =>
                await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken));

            // assert
            exception.Errors.Select(BatchSchemaErrorSnapshot.Create)
                .MatchSnapshot(postFix: regularKey ?? "unkeyed");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Schema_Should_ValidateDataOrderAndDuplicates_When_BatchMiddlewareIsRegistered(
        bool duplicate)
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQL().AddQueryType(d =>
        {
            var field = Batch(d.Field("value")).Extend().Configuration;
            field.BatchMiddlewareConfigurations.Add(new(next => next, key: WellKnownMiddleware.Sorting));
            field.BatchMiddlewareConfigurations.Add(new(next => next,
                key: duplicate ? WellKnownMiddleware.Sorting : WellKnownMiddleware.Paging));
        });

        // act
        var exception = await Assert.ThrowsAsync<SchemaException>(async () =>
            await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken));

        // assert
        exception.Errors.Select(BatchSchemaErrorSnapshot.Create)
            .MatchSnapshot(postFix: duplicate.ToString());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Schema_Should_AcceptEitherFilterSortOrder_When_ValidatingNativePipelines(
        bool batch,
        bool sortingFirst)
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQL().AddQueryType(d =>
        {
            var field = d.Field("value").Type<StringType>();
            if (batch)
            {
                Batch(field);
            }
            else
            {
                field.Resolve("value");
            }

            string[] keys =
            [
                WellKnownMiddleware.DbContext,
                WellKnownMiddleware.Paging,
                WellKnownMiddleware.Projection,
                sortingFirst ? WellKnownMiddleware.Sorting : WellKnownMiddleware.Filtering,
                sortingFirst ? WellKnownMiddleware.Filtering : WellKnownMiddleware.Sorting
            ];

            var configuration = field.Extend().Configuration;
            foreach (var key in keys)
            {
                if (batch)
                {
                    configuration.BatchMiddlewareConfigurations.Add(new(next => next, key: key));
                }
                else
                {
                    configuration.MiddlewareConfigurations.Add(new(next => next, key: key));
                }
            }
        });

        // act
        var schema = await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.ToString().MatchInlineSnapshot("""
            schema {
              query: Query
            }

            type Query {
              value: String
            }
            """);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Schema_Should_AcceptNativeMiddleware_When_OrderValidationIsDisabled(bool duplicate)
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQL()
            .ModifyOptions(o => o.ValidatePipelineOrder = false)
            .AddQueryType(d =>
            {
                var field = Batch(d.Field("value")).Extend().Configuration;
                field.BatchMiddlewareConfigurations.Add(new(next => next, key: WellKnownMiddleware.Sorting));
                field.BatchMiddlewareConfigurations.Add(new(next => next,
                    key: duplicate ? WellKnownMiddleware.Sorting : WellKnownMiddleware.Paging));
            });

        // act
        var schema = await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.ToString().MatchInlineSnapshot("""
            schema {
              query: Query
            }

            type Query {
              value: String
            }
            """);
    }

    [Fact]
    public async Task Schema_Should_IgnoreGlobalMiddleware_When_FieldIsBatchResolved()
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQL()
            .UseField(next => async context =>
            {
                await next(context);
                context.Result = "regular";
            })
            .AddQueryType(d =>
            {
                Batch(d.Field("batch"));
                d.Field("regular").Resolve("value");
            });

        // act
        await using var result = await builder.ExecuteRequestAsync("{ batch regular }",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot("""
            {
              "data": {
                "batch": "value",
                "regular": "regular"
              }
            }
            """);
    }

    [Fact]
    public async Task Schema_Should_RejectMappedMiddleware_When_FieldIsBatchResolved()
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQL()
            .MapField<WrapMiddleware>(new FieldReference("Query", "value"))
            .AddQueryType(d => Batch(d.Field("value")));

        // act
        var exception = await Assert.ThrowsAsync<SchemaException>(async () =>
            await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken));

        // assert
        exception.Errors.Select(BatchSchemaErrorSnapshot.Create).MatchSnapshot();
    }

    [Fact]
    public async Task Schema_Should_RejectDataLoaderMiddleware_When_FieldIsBatchResolved()
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQL().AddQueryType(d =>
        {
            var field = Batch(d.Field("value"));
            field.Extend().Configuration.ResultType = typeof(string);
            field.UseDataLoader<CacheDataLoader<string, string>>();
        });

        // act
        var exception = await Assert.ThrowsAsync<SchemaException>(async () =>
            await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken));

        // assert
        exception.Errors.Select(BatchSchemaErrorSnapshot.Create).MatchSnapshot();
    }

    [Theory]
    [InlineData(WellKnownMiddleware.Paging)]
    [InlineData(WellKnownMiddleware.Projection)]
    [InlineData(WellKnownMiddleware.Filtering)]
    [InlineData(WellKnownMiddleware.Sorting)]
    [InlineData(WellKnownMiddleware.DbContext)]
    [InlineData("Query Results")]
    [InlineData("PromiseCachePagePublisher")]
    [InlineData(WellKnownMiddleware.ToList)]
    [InlineData(WellKnownMiddleware.SingleOrDefault)]
    public async Task Schema_Should_ReportDisplayName_When_KeyHasNoNativeCounterpart(string key)
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQL().AddQueryType(d =>
        {
            Batch(d.Field("value")).Extend().Configuration.MiddlewareConfigurations
                .Add(new(next => next, key: key));
            Batch(d.Field("other")).Extend().Configuration.BatchMiddlewareConfigurations
                .Add(new(next => next, key: key));
        });

        // act
        var exception = await Assert.ThrowsAsync<SchemaException>(async () =>
            await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken));

        // assert
        exception.Errors.Select(BatchSchemaErrorSnapshot.Create).MatchSnapshot(postFix: key);
    }

    [Fact]
    public async Task Schema_Should_DeduplicateNonRepeatableMiddleware_When_PairingConfigurations()
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQL().AddQueryType(d =>
        {
            var field = Batch(d.Field("value")).Extend().Configuration;
            field.MiddlewareConfigurations.Add(new(next => next, isRepeatable: false, key: "custom"));
            field.MiddlewareConfigurations.Add(new(next => next, isRepeatable: false, key: "custom"));
        });

        // act
        var exception = await Assert.ThrowsAsync<SchemaException>(async () =>
            await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken));

        // assert
        exception.Errors.Select(BatchSchemaErrorSnapshot.Create).MatchSnapshot();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Schema_Should_PreservePairing_When_InterfaceMiddlewareIsInherited(bool paired)
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQL()
            .AddQueryType(d => d.Field("items").Resolve(new[] { new ValidationItem(1) }))
            .AddObjectType<ValidationItem>(d => d.Implements<InterfaceType<IValidationItem>>())
            .AddInterfaceType<IValidationItem>(d =>
            {
                var field = d.Field("value").Type<StringType>()
                    .ResolveBatchWith<UnsupportedResolvers>(r => r.GetValue()).Extend().Configuration;
                field.MiddlewareDefinitions.Add(new(next => next, key: "custom"));
                if (paired)
                {
                    field.BatchMiddlewareConfigurations.Add(new(next => next, key: "custom"));
                }
            });

        // act
        if (paired)
        {
            await using var result = await builder.ExecuteRequestAsync("{ items { value } }",
                cancellationToken: TestContext.Current.CancellationToken);

            // assert
            result.MatchInlineSnapshot("""
                {
                  "data": {
                    "items": [
                      {
                        "value": "value"
                      }
                    ]
                  }
                }
                """);
        }
        else
        {
            var exception = await Assert.ThrowsAsync<SchemaException>(async () =>
                await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken));

            // assert
            exception.Errors.Select(BatchSchemaErrorSnapshot.Create).MatchSnapshot();
        }
    }

    private static IObjectFieldDescriptor Batch(IObjectFieldDescriptor field)
        => field.Type<StringType>().ResolveBatch(contexts =>
            new ValueTask<IReadOnlyList<ResolverResult>>(
                contexts.Select(_ => ResolverResult.Ok("value")).ToArray()));

    public sealed class WrapMiddleware(FieldDelegate next)
    {
        public ValueTask InvokeAsync(IMiddlewareContext context) => next(context);
    }

    public interface IValidationItem
    {
        int Id { get; }
    }

    public sealed record ValidationItem(int Id) : IValidationItem;

    public sealed class UnsupportedResolvers
    {
        [BatchResolver, UseWrap]
        public IReadOnlyList<string> GetUnkeyed() => ["value"];

        [BatchResolver, UseKeyed]
        public IReadOnlyList<string> GetKeyed() => ["value"];

        [BatchResolver, UseDirective]
        public IReadOnlyList<string> GetDirective() => ["value"];

        [BatchResolver]
        public IReadOnlyList<string> GetValue() => ["value"];
    }

    public sealed class UseWrapAttribute : ObjectFieldDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo? member)
            => descriptor.Use(next => next);
    }

    public sealed class UseKeyedAttribute : ObjectFieldDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo? member)
            => descriptor.Extend().Configuration.MiddlewareConfigurations.Add(new(next => next, key: "custom"));
    }

    public sealed class UseDirectiveAttribute : ObjectFieldDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo? member)
            => descriptor.Directive("wrap");
    }
}

internal static class BatchSchemaErrorSnapshot
{
    public static object Create(ISchemaError error)
        => new
        {
            error.Code,
            error.Message,
            Extensions = error.Extensions?.OrderBy(t => t.Key, StringComparer.Ordinal)
                .ToDictionary(t => t.Key, t => t.Value)
        };
}
