using HotChocolate.Types;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class JsonResultBuilderGenerator
    {
        private void AddArrayHandler(
            ClassBuilder classBuilder,
            MethodBuilder methodBuilder,
            ListTypeDescriptor listTypeDescriptor
            )
        {
            var listVarName = listTypeDescriptor.Name.WithLowerFirstChar() + "s";

            string elementType = listTypeDescriptor.IsEntityType()
                ? TypeNames.EntityId
                : listTypeDescriptor.InnerType.Name;

            methodBuilder.AddCode(
                $"var {listVarName} = new {TypeNames.List}<{elementType}>();");

            methodBuilder.AddCode(
                ForEachBuilder.New()
                    .SetLoopHeader($"{TypeNames.JsonElement} child in {_objParamName}.Value.EnumerateArray()")
                    .AddCode(
                        MethodCallBuilder.New()
                            .SetPrefix($"{listVarName}.")
                            .SetMethodName("Add")
                            .AddArgument(
                                BuildUpdateMethodCall(listTypeDescriptor.InnerType, "child"))));

            methodBuilder.AddEmptyLine();
            methodBuilder.AddCode($"return {listVarName};");

            AddDeserializeMethod(listTypeDescriptor.InnerType, classBuilder);
        }
    }
}
