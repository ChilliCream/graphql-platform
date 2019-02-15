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
            Request = request;
            Document = Parser.Default.Parse(request.Query);
            Promise = new TaskCompletionSource<IExecutionResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public IReadOnlyQueryRequest Request { get; }
        public DocumentNode Document { get; }
        public TaskCompletionSource<IExecutionResult> Promise { get; }
        public IDictionary<string, string> Aliases { get; set; }
    }
}
