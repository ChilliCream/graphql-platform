using System.Text.Json.Serialization;
using Json.Schema;

namespace HotChocolate.ModelContextProtocol.JsonSerializerContexts;

[JsonSerializable(typeof(JsonSchema))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization)]
internal partial class JsonSchemaJsonSerializerContext : JsonSerializerContext;
