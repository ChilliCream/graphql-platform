using System.Linq;
using System.Text.Json;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.Serialization.BuiltInTypeNames;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class JsonResultBuilderGenerator
    {
        private void AddScalarTypeDeserializerMethod(
            MethodBuilder methodBuilder,
            NamedTypeDescriptor namedType)
        {
            string deserializeMethod = namedType.GraphQLTypeName?.Value switch
            {
                String => nameof(JsonElement.GetString),
                ID => nameof(JsonElement.GetString),
                Url => nameof(JsonElement.GetString),
                Uuid => nameof(JsonElement.GetString),
                DateTime => nameof(JsonElement.GetString),
                Date => nameof(JsonElement.GetString),
                TimeSpan => nameof(JsonElement.GetString),
                Boolean => nameof(JsonElement.GetBoolean),
                Byte => nameof(JsonElement.GetByte),
                Short => nameof(JsonElement.GetInt16),
                Int => nameof(JsonElement.GetInt32),
                Long => nameof(JsonElement.GetInt64),
                Float => nameof(JsonElement.GetDouble),
                Decimal => nameof(JsonElement.GetDecimal),
                ByteArray => nameof(JsonElement.GetBytesFromBase64),
                _ => "Get" + (namedType.SerializationType?.Split('.').Last() ??
                    namedType.Name.WithCapitalFirstChar())
            };

            methodBuilder.AddCode(
                $"return {namedType.Name.ToFieldName()}Parser.Parse({_objParamName}.Value" +
                $".{deserializeMethod}()!);");
        }
    }
}
