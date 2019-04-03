using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types;
using Moq;
using Xunit;

namespace HotChocolate.Subscriptions
{
    public class SubscriptionTests
    {
        [Fact]
        public async Task Subscribe_RaiseEvent_ReceiveSubscriptionResult()
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
            IReadOnlyQueryResult result = await responseStream.ReadAsync();
            Assert.False(responseStream.IsCompleted);
            Assert.Equal("bar", result.Data["foo"]);
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
                    ctx.Service<IEventSender>()
                        .SendAsync(new EventMessage("foo"));
                    return "barmut";
                });
            }
        }
    }
}
