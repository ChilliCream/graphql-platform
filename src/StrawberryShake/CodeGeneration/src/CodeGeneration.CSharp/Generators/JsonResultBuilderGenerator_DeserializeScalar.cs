using System;
using System.Text.Json;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public partial class JsonResultBuilderGenerator
    {
        private void AddScalarTypeDeserializerMethod(
            MethodBuilder methodBuilder,
            ILeafTypeDescriptor namedType)
        {
            string deserializeMethod = JsonUtils.GetParseMethod(namedType.SerializationType);

            methodBuilder.AddCode(
                MethodCallBuilder
                    .New()
                    .SetReturn()
                    .SetMethodName(GetFieldName(namedType.Name) + "Parser", "Parse")
                    .AddArgument(MethodCallBuilder
                        .Inline()
                        .SetMethodName(_obj, nameof(Nullable<JsonElement>.Value), deserializeMethod)
                        .SetNullForgiving()));
        }
    }
}
