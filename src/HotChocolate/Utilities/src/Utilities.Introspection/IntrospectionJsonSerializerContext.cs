using System.Text.Json.Serialization;

namespace HotChocolate.Utilities.Introspection;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(IntrospectionResult))]
[JsonSerializable(typeof(IntrospectionData))]
[JsonSerializable(typeof(IntrospectionError))]
[JsonSerializable(typeof(IntrospectionError[]))]
[JsonSerializable(typeof(Schema))]
[JsonSerializable(typeof(FullType))]
[JsonSerializable(typeof(Field))]
[JsonSerializable(typeof(InputField))]
[JsonSerializable(typeof(Directive))]
[JsonSerializable(typeof(TypeRef))]
[JsonSerializable(typeof(RootTypeRef))]
[JsonSerializable(typeof(EnumValue))]
[JsonSerializable(typeof(TypeKind))]
internal sealed partial class IntrospectionJsonSerializerContext : JsonSerializerContext;
