using System;
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
            string deserializeMethod = namedType.SerializationType switch
            {
                TypeNames.String => nameof(JsonElement.GetString),
                TypeNames.Uri => nameof(JsonElement.GetString),
                TypeNames.Byte => nameof(JsonElement.GetByte),
                TypeNames.ByteArray => nameof(JsonElement.GetBytesFromBase64),
                TypeNames.Int16 => nameof(JsonElement.GetInt16),
                TypeNames.Int32 => nameof(JsonElement.GetInt32),
                TypeNames.Int64 => nameof(JsonElement.GetInt64),
                TypeNames.UInt16 => nameof(JsonElement.GetUInt16),
                TypeNames.UInt32 => nameof(JsonElement.GetUInt32),
                TypeNames.UInt64 => nameof(JsonElement.GetUInt64),
                TypeNames.Single => nameof(JsonElement.GetSingle),
                TypeNames.Double => nameof(JsonElement.GetDouble),
                TypeNames.Decimal => nameof(JsonElement.GetDecimal),
                TypeNames.DateTimeOffset => nameof(JsonElement.GetString),
                TypeNames.DateTime => nameof(JsonElement.GetString),
                TypeNames.TimeSpan => nameof(JsonElement.GetString),
                TypeNames.Boolean => nameof(JsonElement.GetBoolean),
                _ => throw new NotSupportedException("Serialization format not supported.")
            };

            methodBuilder.AddCode(
                $"return {namedType.Name.ToFieldName()}Parser.Parse({_objParamName}.Value" +
                $".{deserializeMethod}()!);");
        }
    }
}
