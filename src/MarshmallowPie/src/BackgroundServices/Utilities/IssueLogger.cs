using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarshmallowPie.Processing;

namespace MarshmallowPie.BackgroundServices
{
    public sealed class IssueLogger
    {
        private readonly List<Issue> _issues = new List<Issue>();
        private readonly string _sessionId;
        private readonly IMessageSender<PublishDocumentEvent> _eventSender;

        public IssueLogger(
            string sessionId,
            IMessageSender<PublishDocumentEvent> eventSender)
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
                new PublishDocumentEvent(_sessionId, issue),
                cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task LogIssuesAsync(
            IEnumerable<Issue> issues,
            CancellationToken cancellationToken = default)
        {
            Issue[] localIssues = issues.ToArray();

            _issues.AddRange(localIssues);

            foreach (Issue issue in localIssues)
            {
                await _eventSender.SendAsync(
                    new PublishDocumentEvent(_sessionId, issue),
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
