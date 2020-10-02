using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Client
{
    internal class BufferedRequest
    {
        public BufferedRequest(IReadOnlyQueryRequest request)
        {
            Promise = new TaskCompletionSource<IExecutionResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            Request = request;
            Document = request.Query is QueryDocument document
                ? document.Document
                : Utf8GraphQLParser.Parse(request.Query!.AsSpan());
        }

        public IReadOnlyQueryRequest Request { get; }

        public DocumentNode Document { get; }

        public TaskCompletionSource<IExecutionResult> Promise { get; }

        public IDictionary<string, string> Aliases { get; set; }
    }
}
