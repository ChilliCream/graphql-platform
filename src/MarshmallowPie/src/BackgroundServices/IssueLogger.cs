using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MarshmallowPie.Processing;

namespace MarshmallowPie.BackgroundServices
{
    internal sealed class IssueLogger
    {
        private readonly List<Issue> _issues = new List<Issue>();
        private readonly string _sessionId;
        private readonly IMessageSender<PublishSchemaEvent> _eventSender;

        public IssueLogger(
            string sessionId,
            IMessageSender<PublishSchemaEvent> eventSender)
        {
            _sessionId = sessionId;
            _eventSender = eventSender;
        }

        public IReadOnlyList<Issue> Issues => _issues;

        public async Task LogIssueAsync(
            Issue issue,
            CancellationToken cancellationToken = default)
        {
            _issues.Add(issue);

            await _eventSender.SendAsync(
                new PublishSchemaEvent(_sessionId, issue),
                cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
