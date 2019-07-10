using HotChocolate.Configuration;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal static class WebSocketContextHelper
    {
        internal static (WebSocketContext, WebSocketMock) Create()
        {
            var httpContext = new HttpContextMock();
            var webSocket = new WebSocketMock();
            IQueryExecutor queryExecutor = SchemaBuilder.New()
                .SetOptions(new SchemaOptions { StrictValidation = false })
                .Create()
                .MakeExecutable();

            return (new WebSocketContext(httpContext, webSocket, queryExecutor,
                null, null), webSocket);
        }
    }
}