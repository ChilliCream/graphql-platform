namespace StrawberryShake.CodeGeneration.CSharp
{
    public abstract class CSharpCodeGenerator<TDescriptor>
        : CodeGenerator<TDescriptor>
        where TDescriptor : ICodeDescriptor
    {
        protected bool NullableRefTypes { get; } = false;

        protected WellKnownTypes Types { get; } = new WellKnownTypes();

        protected class WellKnownTypes
        {
            public string ArgumentNullException { get; } =
                "global::System.ArgumentNullException";

            public string JsonElement {get;} =
                "global::System.Text.Json.JsonElement";

            public string ValueSerializerCollection { get; } =
                "global::StrawberryShake.IValueSerializerCollection";

            public string ValueSerializer { get; } =
                "global::StrawberryShake.IValueSerializer";

            public string JsonResultParserBase { get; } =
                "global::StrawberryShake.Http.JsonResultParserBase";
        }
    }
}
