using HotChocolate.Types;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class JsonResultBuilderGenerator
    {
        private void AddUpdateEntityArrayMethod(
            ListTypeDescriptor listTypeDescriptor,
            ITypeDescriptor originalTypeDescriptor,
            ClassBuilder classBuilder)
        {
            var updateEntityMethod = MethodBuilder.New()
                .SetAccessModifier(AccessModifier.Private)
                .SetName(DeserializerMethodNameFromTypeName(originalTypeDescriptor))
                .SetReturnType($"{TypeNames.IList}<{TypeNames.EntityId}>")
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType(_jsonElementParamName)
                        .SetName(_objParamName))
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType($"{TypeNames.ISet}<{TypeNames.EntityId}>")
                        .SetName(_entityIdsParam));

            updateEntityMethod.AddCode(
                EnsureJsonValueIsNotNull(),
                originalTypeDescriptor.IsNonNullableType());

            var listVarName = listTypeDescriptor.Name.WithLowerFirstChar() + "s";

            string elementType = listTypeDescriptor.IsEntityType()
                ? TypeNames.EntityId
                : listTypeDescriptor.InnerType.Name;

            updateEntityMethod.AddCode(
                $"var {listVarName} = new {TypeNames.List}<{elementType}>();");

            updateEntityMethod.AddCode(
                ForEachBuilder.New()
                    .SetLoopHeader($"{TypeNames.JsonElement} child in {_objParamName}.EnumerateArray()")
                    .AddCode(
                        MethodCallBuilder.New()
                            .SetPrefix($"{listVarName}.")
                            .SetMethodName("Add")
                            .AddArgument(
                                BuildUpdateMethodCall(listTypeDescriptor.InnerType, "child"))));

            updateEntityMethod.AddEmptyLine();
            updateEntityMethod.AddCode($"return {listVarName};");

            classBuilder.AddMethod(updateEntityMethod);

            AddDeserializeMethod(listTypeDescriptor.InnerType, classBuilder);
        }
    }
}
