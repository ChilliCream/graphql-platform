namespace StrawberryShake.CodeGeneration.CSharp
{
    public abstract class CSharpCodeGenerator<TDescriptor>
        : CodeGenerator<TDescriptor>
        where TDescriptor : ICodeDescriptor
    {
        protected bool NullableRefTypes { get; } = true;

        protected WellKnownTypes Types { get; } = new WellKnownTypes();

        protected class WellKnownTypes
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
            public string ValueSerializerCollection { get; } =
                "global::StrawberryShake.IValueSerializerCollection";

            public string ValueSerializer { get; } =
                "global::StrawberryShake.IValueSerializer";

            public string ValueKind { get; } =
                "global::StrawberryShake.ValueKind";

            public string JsonResultParserBase { get; } =
                "global::StrawberryShake.Http.JsonResultParserBase";
        }
    }
}
