using System.Text.Json;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public partial class JsonResultBuilderGenerator
{
    private void AddScalarTypeDeserializerMethod(
        MethodBuilder methodBuilder,
        ILeafTypeDescriptor namedType)
    {
        var methodCall = MethodCallBuilder
            .New()
            .SetReturn()
            .SetMethodName(GetFieldName(namedType.Name) + "Parser", "Parse");

        if (namedType.SerializationType.ToString() == TypeNames.JsonElement)
        {
            methodCall.AddArgument($"{_obj}.{nameof(Nullable<JsonElement>.Value)}!");
        }
        else
        {
            var deserializeMethod = JsonUtils.GetParseMethod(namedType.SerializationType);
            methodCall.AddArgument(MethodCallBuilder
                .Inline()
                .SetMethodName(_obj, nameof(Nullable<JsonElement>.Value), deserializeMethod)
                .SetNullForgiving());
        }

        methodBuilder.AddCode(methodCall);
    }
}
