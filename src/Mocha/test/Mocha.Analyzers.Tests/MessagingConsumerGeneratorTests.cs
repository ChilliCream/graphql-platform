namespace Mocha.Analyzers.Tests;

public class MessagingConsumerGeneratorTests
{
    [Fact]
    public async Task Generate_SingleConsumer_MatchesSnapshot()
    {
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha;

            namespace TestApp;

            public record AuditLogMessage(string Action);

            public class AuditLogConsumer : IConsumer<AuditLogMessage>
            {
                public ValueTask ConsumeAsync(IConsumeContext<AuditLogMessage> context)
                    => default;
            }
            """
        ]).MatchMarkdownAsync();
    }
}
