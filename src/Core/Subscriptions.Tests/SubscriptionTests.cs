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
                c.RegisterServiceProvider(services.Object);
                c.RegisterMutationType<MutationType>();
                c.RegisterSubscriptionType<SubscriptionType>();
            });

            var responseStream =
                await schema.ExecuteAsync("subscription { foo }")
                as IResponseStream;

            // act
            await schema.ExecuteAsync("mutation { foo }");

            // assert
            IQueryExecutionResult result = await responseStream.ReadAsync();
            Assert.False(responseStream.IsCompleted);
            Assert.Equal("bar", result.Data["foo"]);
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
