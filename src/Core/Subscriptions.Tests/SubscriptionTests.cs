using System.Threading;
using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Moq;
using Xunit;

namespace HotChocolate.Subscriptions
{
    public class SubscriptionTests
    {
        [Fact]
        public async Task Subscribe_RaiseEvent_No_Arguments()
        {
            // arrange
            var registry = new InMemoryEventRegistry();

            var services = new Mock<IServiceProvider>();
            services.Setup(t => t.GetService(It.IsAny<Type>()))
                .Returns(new Func<Type, object>(t =>
                {
                    if (t == typeof(IEventRegistry)
                        || t == typeof(IEventSender))
                    {
                        return registry;
                    }
                    return null;
                }));

            ISchema schema = Schema.Create(c =>
            {
                c.RegisterQueryType<DummyQuery>();
                c.RegisterServiceProvider(services.Object);
                c.RegisterMutationType<MutationType>();
                c.RegisterSubscriptionType<SubscriptionType>();
            });

            IQueryExecutor executor = schema.MakeExecutable();

            var responseStream =
                await executor.ExecuteAsync("subscription { foo }")
                as IResponseStream;

            // act
            await executor.ExecuteAsync("mutation { foo }");

            // assert
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            {
                IReadOnlyQueryResult result = await responseStream.ReadAsync(cts.Token);
                Assert.False(responseStream.IsCompleted);
                Assert.Equal("bar", result.Data["foo"]);
            }
        }

        [Fact]
        public async Task Subscribe_RaiseEvent_With_Argument()
        {
            // arrange
            var registry = new InMemoryEventRegistry();

            var services = new Mock<IServiceProvider>();
            services.Setup(t => t.GetService(It.IsAny<Type>()))
                .Returns(new Func<Type, object>(t =>
                {
                    if (t == typeof(IEventRegistry)
                        || t == typeof(IEventSender))
                    {
                        return registry;
                    }
                    return null;
                }));

            ISchema schema = Schema.Create(c =>
            {
                c.RegisterQueryType<DummyQuery>();
                c.RegisterServiceProvider(services.Object);
                c.RegisterMutationType<MutationType>();
                c.RegisterSubscriptionType<SubscriptionType>();
            });

            IQueryExecutor executor = schema.MakeExecutable();

            var responseStream =
                await executor.ExecuteAsync("subscription { bar(baz:\"123\") }")
                as IResponseStream;

            // act
            await executor.ExecuteAsync("mutation { bar(baz:\"123\") }");

            // assert
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            {
                IReadOnlyQueryResult result = await responseStream.ReadAsync(cts.Token);
                Assert.False(responseStream.IsCompleted);
                Assert.Equal("123", result.Data["bar"]);
            }
        }

        [Fact]
        public async Task Subscribe_RaiseEvent_With_Argument_As_Variables()
        {
            // arrange
            var registry = new InMemoryEventRegistry();

            var services = new Mock<IServiceProvider>();
            services.Setup(t => t.GetService(It.IsAny<Type>()))
                .Returns(new Func<Type, object>(t =>
                {
                    if (t == typeof(IEventRegistry)
                        || t == typeof(IEventSender))
                    {
                        return registry;
                    }
                    return null;
                }));

            ISchema schema = Schema.Create(c =>
            {
                c.RegisterQueryType<DummyQuery>();
                c.RegisterServiceProvider(services.Object);
                c.RegisterMutationType<MutationType>();
                c.RegisterSubscriptionType<SubscriptionType>();
            });

            IQueryExecutor executor = schema.MakeExecutable();

            var responseStream =
                await executor.ExecuteAsync(QueryRequestBuilder.New()
                    .SetQuery("subscription($a: String!) { bar(baz:$a) }")
                    .AddVariableValue("a", "123")
                    .Create())
                as IResponseStream;

            // act
            await executor.ExecuteAsync(QueryRequestBuilder.New()
                .SetQuery("mutation($a: String!) { bar(baz:$a) }")
                .AddVariableValue("a", "123")
                .Create());

            // assert
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            {
                IReadOnlyQueryResult result = await responseStream.ReadAsync(cts.Token);
                Assert.False(responseStream.IsCompleted);
                Assert.Equal("123", result.Data["bar"]);
            }
        }

        public class DummyQuery
        {
            public string Foo { get; set; }
        }

        public class SubscriptionType
            : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("subscription");
                descriptor.Field("foo").Resolver(() => "bar");
                descriptor.Field("bar")
                    .Argument("baz", a => a.Type<NonNullType<StringType>>())
                    .Resolver(ctx => ctx.Argument<string>("baz"));
            }
        }

        public class MutationType
            : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("mutation");
                descriptor.Field("foo").Resolver(ctx =>
                {
                    ctx.Service<IEventSender>().SendAsync(new EventMessage("foo"));
                    return "barmut";
                });

                descriptor.Field("bar")
                    .Argument("baz", a => a.Type<NonNullType<StringType>>())
                    .Resolver(ctx =>
                {
                    IValueNode argumentValue = ctx.Argument<IValueNode>("baz");
                    ctx.Service<IEventSender>().SendAsync(
                        new EventMessage("bar", new ArgumentNode("baz", argumentValue)));
                    return "barmut";
                });
            }
        }
    }
}
