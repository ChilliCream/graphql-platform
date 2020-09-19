using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.RateLimit;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.RateLimit.Tests
{
    public class LimitProcessorTests
    {
        [Fact]
        public async Task GivenNoLimit_WhenExecute_ShouldSaveNewLimit()
        {
            // Arrange
            string limitKey = default!;
            Limit limit = default!;
            TimeSpan expiration = default!;
            var limitStore = new Mock<ILimitStore>();
            limitStore.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<Limit>(), default))
                .Callback((string k, TimeSpan e, Limit l, CancellationToken _) =>
                {
                    limitKey = k;
                    expiration = e;
                    limit = l;
                });
            limitStore.Setup(x => x.TryGetAsync(It.IsAny<string>(), default))
                .ReturnsAsync(() => default);
            var limitProcessor = new LimitProcessor(limitStore.Object);
            var policyIdentifiers = new IPolicyIdentifier[] { new ClaimsPolicyIdentifier("sub") };

            // Act
            await limitProcessor.ExecuteAsync(RequestIdentity.Create("user", "1"),
                new LimitPolicy(policyIdentifiers, TimeSpan.FromSeconds(10), 5), default);

            // Assert
            new {limitKey, limit, expiration}.MatchSnapshot();
            //limitKey.Should().Be("1-user");
            //limit.Requests.Should().Be(1);
            //limit.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            //expiration.Should().Be(TimeSpan.FromSeconds(10));
        }

        [Fact]
        public async Task GivenExpiredLimit_WhenExecute_ShouldSaveNewLimit()
        {
            // Arrange
            string limitKey = default!;
            Limit limit = default!;
            TimeSpan expiration = default!;
            var limitStore = new Mock<ILimitStore>();
            limitStore.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<Limit>(), default))
                .Callback((string k, TimeSpan e, Limit l, CancellationToken _) =>
                {
                    limitKey = k;
                    expiration = e;
                    limit = l;
                });
            limitStore.Setup(x => x.TryGetAsync(It.IsAny<string>(), default))
                .ReturnsAsync(() => Limit.Create(DateTime.UtcNow.AddMinutes(-1), 1));
            var limitProcessor = new LimitProcessor(limitStore.Object);
            var policyIdentifiers = new IPolicyIdentifier[] { new ClaimsPolicyIdentifier("sub") };

            // Act
            await limitProcessor.ExecuteAsync(RequestIdentity.Create("user", "1"),
                new LimitPolicy(policyIdentifiers, TimeSpan.FromSeconds(10), 5), default);

            // Assert
            new { limitKey, limit, expiration }.MatchSnapshot();
            //limitKey.Should().Be("1-user");
            //limit.Requests.Should().Be(1);
            //limit.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            //expiration.Should().Be(TimeSpan.FromSeconds(10));
        }

        [Fact]
        public async Task GivenValidLimit_WhenExecute_ShouldIncreaseLimitRequests()
        {
            // Arrange
            string limitKey = default!;
            Limit limit = default!;
            TimeSpan expiration = default!;
            var limitStore = new Mock<ILimitStore>();
            limitStore.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<Limit>(), default))
                .Callback((string k, TimeSpan e, Limit l, CancellationToken _) =>
                {
                    limitKey = k;
                    expiration = e;
                    limit = l;
                });
            limitStore.Setup(x => x.TryGetAsync(It.IsAny<string>(), default))
                .ReturnsAsync(() => Limit.Create(DateTime.UtcNow.AddMinutes(-1), 1));
            var limitProcessor = new LimitProcessor(limitStore.Object);
            var policyIdentifiers = new IPolicyIdentifier[] { new ClaimsPolicyIdentifier("sub") };

            // Act
            await limitProcessor.ExecuteAsync(RequestIdentity.Create("user", "1"),
                new LimitPolicy(policyIdentifiers, TimeSpan.FromMinutes(2), 5), default);

            // Assert
            new { limitKey, limit, expiration }.MatchSnapshot();
            //limitKey.Should().Be("1-user");
            //limit.Requests.Should().Be(2);
            //limit.Timestamp.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(-1), TimeSpan.FromSeconds(1));
            //expiration.Should().Be(TimeSpan.FromMinutes(2));
        }
    }
}
