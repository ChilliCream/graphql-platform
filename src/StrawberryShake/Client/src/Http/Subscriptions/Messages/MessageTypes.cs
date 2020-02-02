namespace StrawberryShake.Http.Subscriptions.Messages
{
    internal static class MessageTypes
    {
        internal static class Connection
        {
            public const string Initialize = "connection_init";
            public const string Accept = "connection_ack";
            public const string Error = "connection_error";
            public const string KeepAlive = "ka";
            public const string Terminate = "connection_terminate";
        }

        public static class Subscription
        {
            public const string Start = "start";
            public const string Data = "data";
            public const string Error = "error";
            public const string Complete = "complete";
            public const string Stop = "stop";
        }
    }
}
