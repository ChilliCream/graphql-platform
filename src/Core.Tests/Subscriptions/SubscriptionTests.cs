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
        public async Task Foo()
        {
            InMemoryEventRegistry registry = new InMemoryEventRegistry();

            Mock<IServiceProvider> services = new Mock<IServiceProvider>();
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

            IExecutionResult result = await schema.ExecuteAsync("subscription { foo }");
            await schema.ExecuteAsync("mutation { foo }");

            if (result is ISubscriptionExecutionResult sr)
            {
                bool moveNext = await sr.MoveNextAsync();
                Assert.True(moveNext);
                Assert.Equal("bar", sr.Current.Data["foo"]);
            }
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
                    ctx.Service<IEventSender>().SendAsync(new Event("foo"));
                    return "barmut";
                });
            }
        }
    }
}
