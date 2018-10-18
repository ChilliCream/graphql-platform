using HotChocolate.Execution;
using Newtonsoft.Json.Linq;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal static class MessageTypes
    {
        internal static class Connection
        {
            public static readonly string Initialize = "connection_init";
            public static readonly string Accept = "connection_ack";
            public static readonly string Error = "connection_error";
            public static readonly string KeepAlive = "ka";
            public static readonly string Terminate = "connection_terminate";
        }

        public static class Subscription
        {
            public static readonly string Start = "start";
            public static readonly string Data = "data";
            public static readonly string Error = "error";
            public static readonly string Complete = "complete";
            public static readonly string Stop = "stop";
        }
    }
}
