using System.Collections.Immutable;
using System.Reflection;
using CookieCrumble;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Execution;

public class BatchDirectiveMiddlewareTests
{
    [Theory]
    [InlineData(false, "delegate")]
    [InlineData(false, "class")]
    [InlineData(false, "factory")]
    [InlineData(false, "type")]
    [InlineData(true, "delegate")]
    [InlineData(true, "class")]
    [InlineData(true, "factory")]
    public async Task UseBatch_Should_BindEachDirectiveAndComposeInOrder_When_RegisteredRepeatedly(
        bool generic,
        string registration)
    {
        // arrange
        var events = new List<string>();
        var service = new MarkerService(events);
        var directiveType = generic
            ? new DirectiveType<Mark>(d =>
            {
                d.Name("mark").Location(DirectiveLocation.FieldDefinition).Repeatable();
                d.Use((FieldDelegate next, Directive _) => next);
                switch (registration)
                {
                    case "delegate":
                        d.UseBatch(CreateMiddleware);
                        break;
                    case "class":
                        d.UseBatch<DirectiveMiddlewareClass>();
                        break;
                    case "factory":
                        d.UseBatch((sp, next) => new DirectiveMiddlewareClass(
                            next, sp.GetRequiredService<MarkerService>()));
                        break;
                }

                d.UseBatch((next, directive) => Wrap(next, directive.ToValue<Mark>().Value + "2", events));
            })
            : new DirectiveType(d =>
            {
                d.Name("mark").Location(DirectiveLocation.FieldDefinition).Repeatable();
                d.Argument("value").Type<NonNullType<StringType>>();
                d.Use((FieldDelegate next, Directive _) => next);
                switch (registration)
                {
                    case "delegate":
                        d.UseBatch(CreateMiddleware);
                        break;
                    case "class":
                        d.UseBatch<UntypedDirectiveMiddlewareClass>();
                        break;
                    case "factory":
                        d.UseBatch((sp, next) => new UntypedDirectiveMiddlewareClass(
                            next, sp.GetRequiredService<MarkerService>()));
                        break;
                    case "type":
                        d.UseBatch(BatchDirectiveClassMiddlewareFactory.Create(typeof(UntypedDirectiveMiddlewareClass)));
                        break;
                }

                d.UseBatch((next, directive) => Wrap(next, GetValue(directive) + "2", events));
            });
        var executor = await new ServiceCollection().AddSingleton(service).AddGraphQL()
            .AddDirectiveType(directiveType)
            .AddQueryType(d =>
            {
                foreach (var name in new[] { "first", "second" })
                {
                    var field = d.Field(name).Type<StringType>();
                    field.Directive("mark", new ArgumentNode("value", name == "first" ? "A" : "C"));
                    if (name == "first")
                    {
                        field.Directive("mark", new ArgumentNode("value", "B"));
                    }

                    for (var i = 0; i < 2; i++)
                    {
                        field.Extend().Configuration.BatchMiddlewareConfigurations.Add(new(
                            next => Wrap(next, "field", events), isRepeatable: false, key: "field"));
                    }
                    field.Extend().Configuration.FormatterConfigurations.Add(new((_, value) => $"format({value})"));
                    field.ResolveBatch(contexts => new ValueTask<IReadOnlyList<ResolverResult>>(
                        contexts.Select(_ => ResolverResult.Ok("value")).ToArray()));
                }
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var first = await executor.ExecuteAsync("{ first }",
            cancellationToken: TestContext.Current.CancellationToken);
        await using var second = await executor.ExecuteAsync("{ second }",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot().Add(first, "First field").Add(second, "Second field")
            .Add(events, "Middleware order").MatchMarkdownSnapshot();

        BatchFieldDelegate CreateMiddleware(BatchFieldDelegate next, Directive directive)
            => Wrap(next, generic ? directive.ToValue<Mark>().Value : GetValue(directive), events);
    }

    [Fact]
    public async Task UseBatch_Should_SelectMatchingPipeline_When_DirectiveHasBothMiddlewareKinds()
    {
        // arrange
        var executor = await new ServiceCollection().AddGraphQL()
            .AddDirectiveType(new DirectiveType(d => d.Name("both")
                .Location(DirectiveLocation.FieldDefinition)
                .Use((next, _) => async context =>
                {
                    await next(context);
                    context.Result = $"regular({context.Result})";
                })
                .UseBatch((next, _) => async contexts =>
                {
                    await next(contexts);
                    foreach (var context in contexts)
                    {
                        context.Result = $"batch({context.Result})";
                    }
                })))
            .AddQueryType(d =>
            {
                d.Field("regular").Type<StringType>().Directive("both").Resolve("value");
                d.Field("batch").Type<StringType>().Directive("both")
                    .ResolveBatch(contexts => new ValueTask<IReadOnlyList<ResolverResult>>(
                        contexts.Select(_ => ResolverResult.Ok("value")).ToArray()));
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync("{ regular batch }",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "regular": "regular(value)",
                "batch": "batch(value)"
              }
            }
            """);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("factory")]
    [InlineData("services")]
    [InlineData("type")]
    [InlineData("attribute")]
    public async Task UseBatch_Should_ActivateFieldMiddleware_When_RegisteredThroughSupportedPaths(string registration)
    {
        // arrange
        var service = new MarkerService([]);
        var executor = await new ServiceCollection().AddSingleton(service).AddGraphQL()
            .AddQueryType(d =>
            {
                var field = d.Field("value").Type<StringType>();
                switch (registration)
                {
                    case "class":
                        field.UseBatch<FieldMiddlewareClass>();
                        break;
                    case "factory":
                        field.UseBatch((sp, next) => new FieldMiddlewareClass(
                            next, sp.GetRequiredService<MarkerService>()));
                        break;
                    case "services":
                        field.UseBatch(BatchFieldClassMiddlewareFactory.Create<FieldMiddlewareClass>(
                            (typeof(MarkerService), service)));
                        break;
                    case "type":
                        field.UseBatch(BatchFieldClassMiddlewareFactory.Create(typeof(FieldMiddlewareClass),
                            (typeof(MarkerService), service)));
                        break;
                    case "attribute":
                        field = d.Field(typeof(AttributeQuery).GetMethod(nameof(AttributeQuery.Value))!);
                        break;
                }

                field.ResolveBatch(contexts => new ValueTask<IReadOnlyList<ResolverResult>>(
                    contexts.Select(_ => ResolverResult.Ok("value")).ToArray()));
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var first = await executor.ExecuteAsync("{ value }",
            cancellationToken: TestContext.Current.CancellationToken);
        await using var second = await executor.ExecuteAsync("{ value }",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot().Add(first, "First request").Add(second, "Second request")
            .Add(service.Activations, "Activations").Add(service.Events, "Middleware order")
            .MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UseBatch_Should_PreserveSkipAndFormatting_When_DirectiveReturnsAbstractResults(bool union)
    {
        // arrange
        var observed = new List<int>();
        var dispatched = new List<int>();
        var formatted = new List<int>();
        var executor = await new ServiceCollection().AddGraphQL()
            .AddDirectiveType(new DirectiveType(d => d.Name("cached")
                .Location(DirectiveLocation.FieldDefinition)
                .UseBatch((next, _) => async contexts =>
                {
                    foreach (var context in contexts)
                    {
                        observed.Add(context.Parent<Holder>().Id);
                        if (context.Parent<Holder>().Id == 1)
                        {
                            context.Result = new Cat("cached");
                        }
                    }

                    await next(contexts);
                })))
            .AddInterfaceType<INode>(d => d.Name("Node").Field(n => n.Name))
            .AddUnionType(d => d.Name("Nodes").Type<ObjectType<Cat>>().Type<ObjectType<Dog>>())
            .AddObjectType<Cat>(d => d.Implements<InterfaceType<INode>>())
            .AddObjectType<Dog>(d => d.Implements<InterfaceType<INode>>())
            .AddQueryType(d => d.Field("holders").Type<ListType<ObjectType<Holder>>>()
                .Resolve(new[] { new Holder(1), new Holder(2) }))
            .AddObjectType<Holder>(d =>
            {
                var field = d.Field("node").Type(union ? "Nodes" : "Node").Directive("cached");
                field.Extend().Configuration.FormatterConfigurations.Add(new((context, value) =>
                {
                    formatted.Add(context.Parent<Holder>().Id);
                    return new Dog($"formatted:{((INode)value!).Name}");
                }));
                field.ResolveBatch(contexts =>
                {
                    var results = new ResolverResult[contexts.Count];
                    for (var i = 0; i < contexts.Count; i++)
                    {
                        var id = contexts[i].Parent<Holder>().Id;
                        dispatched.Add(id);
                        results[i] = ResolverResult.Ok(new Dog($"resolved:{id}"));
                    }

                    return new ValueTask<IReadOnlyList<ResolverResult>>(results);
                });
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ holders { node { __typename ... on Cat { name } ... on Dog { name } } } }",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot().Add(result, "Result").Add(observed, "Directive parents")
            .Add(dispatched, "Resolver parents").Add(formatted, "Formatter parents")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task UseBatch_Should_BindConstructorParameters_When_DirectivesHaveDifferentValues()
    {
        // arrange
        var executor = await new ServiceCollection().AddSingleton(new MarkerService([])).AddGraphQL()
            .AddDirectiveType(new DirectiveType<Mark>(d => d.Name("mark")
                .Location(DirectiveLocation.FieldDefinition).Repeatable()
                .UseBatch<ConstructorDirectiveMiddleware>()))
            .AddQueryType(d => d.Field("value").Type<StringType>()
                .Directive(new Mark("A")).Directive(new Mark("B"))
                .ResolveBatch(contexts => new ValueTask<IReadOnlyList<ResolverResult>>(
                    contexts.Select(_ => ResolverResult.Ok("value")).ToArray())))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync("{ value }",
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "value": "A:A:A(B:B:B(value))"
              }
            }
            """);
    }

    private static string GetValue(Directive directive)
        => directive.ToValue<Dictionary<string, object?>>()["value"]!.ToString()!;

    private static BatchFieldDelegate Wrap(BatchFieldDelegate next, string marker, List<string> events)
        => async contexts =>
        {
            events.Add(marker + ":before");
            await next(contexts);
            foreach (var context in contexts)
            {
                context.Result = $"{marker}({context.Result})";
            }

            events.Add(marker + ":after");
        };

    public sealed record Mark(string Value);

    public sealed record Holder(int Id);

    public interface INode
    {
        string Name { get; }
    }

    public sealed record Cat(string Name) : INode;

    public sealed record Dog(string Name) : INode;

    public sealed class ConstructorDirectiveMiddleware(
        BatchFieldDelegate next,
        Mark value,
        Directive directive,
        DirectiveNode syntax,
        MarkerService service)
    {
        public ValueTask InvokeAsync(ImmutableArray<IMiddlewareContext> contexts)
            => Wrap(next,
                $"{value.Value}:{directive.ToValue<Mark>().Value}:{((StringValueNode)syntax.Arguments[0].Value).Value}",
                service.Events)(contexts);
    }

    public sealed class MarkerService(List<string> events)
    {
        public List<string> Events { get; } = events;

        public int Activations { get; set; }
    }

    public sealed class DirectiveMiddlewareClass(BatchFieldDelegate next, MarkerService constructorService)
    {
        public ValueTask InvokeAsync(
            ImmutableArray<IMiddlewareContext> contexts,
            Mark value,
            Directive directive,
            DirectiveNode syntax,
            MarkerService service)
        {
            Assert.Same(constructorService, service);
            Assert.Equal(value.Value, directive.ToValue<Mark>().Value);
            Assert.Equal(value.Value, ((StringValueNode)syntax.Arguments[0].Value).Value);
            return Wrap(next, value.Value, service.Events)(contexts);
        }
    }

    public sealed class UntypedDirectiveMiddlewareClass(BatchFieldDelegate next, MarkerService constructorService)
    {
        public ValueTask InvokeAsync(
            ImmutableArray<IMiddlewareContext> contexts,
            Directive directive,
            DirectiveNode syntax,
            MarkerService service)
        {
            Assert.Same(constructorService, service);
            Assert.Equal(GetValue(directive), ((StringValueNode)syntax.Arguments[0].Value).Value);
            return Wrap(next, GetValue(directive), service.Events)(contexts);
        }
    }

    public sealed class FieldMiddlewareClass
    {
        private readonly BatchFieldDelegate _next;
        private readonly MarkerService _service;

        public FieldMiddlewareClass(BatchFieldDelegate next, MarkerService service)
        {
            _next = next;
            _service = service;
            service.Activations++;
        }

        public ValueTask InvokeAsync(ImmutableArray<IMiddlewareContext> contexts, MarkerService service)
        {
            Assert.Same(_service, service);
            return Wrap(_next, "field", service.Events)(contexts);
        }
    }

    public sealed class AttributeQuery
    {
        [BatchMarker]
        public string Value() => "unused";
    }

    private sealed class BatchMarkerAttribute : ObjectFieldDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo? member)
        {
            descriptor.UseBatch<FieldMiddlewareClass>();
        }
    }
}
