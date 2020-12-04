namespace StrawberryShake.CodeGeneration.CSharp
{
    public class WellKnownTypes
    {
        public string ArgumentNullException { get; } =
            "global::System.ArgumentNullException";

        public string NotSupportedException { get; } =
            "global::System.NotSupportedException";

        public string InvalidOperationException { get; } =
            "global::System.InvalidOperationException";
        public string Type { get; } =
            "global::System.Type";

        public string JsonElement { get; } =
            "global::System.Text.Json.JsonElement";

        public string JsonValueKind { get; } =
            "global::System.Text.Json.JsonValueKind";
        public string IValueSerializerCollection { get; } =
            "global::StrawberryShake.IValueSerializerCollection";

        public string IValueSerializer { get; } =
            "global::StrawberryShake.IValueSerializer";

        public string ValueKind { get; } =
            "global::StrawberryShake.ValueKind";

        public string JsonResultParserBase { get; } =
            "global::StrawberryShake.Http.JsonResultParserBase";

        public string IOperationExecutorPool {get;} =
            "global::StrawberryShake.IOperationExecutorPool";

        public string IOperationExecutor {get;} =
            "global::StrawberryShake.IOperationExecutor";

        public string IOperationStreamExecutor {get;} =
            "global::StrawberryShake.IOperationStreamExecutor";
    }
}
