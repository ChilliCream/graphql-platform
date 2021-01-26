using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class JsonResultBuilderGenerator
    {
        private void AddScalarTypeDeserializerMethod(
            MethodBuilder methodBuilder,
            NamedTypeDescriptor namedType)
        {
            var jsonGetterTypeName =
                namedType.SerializationType?.Split(".").Last()
                ?? namedType.Name.WithCapitalFirstChar();
            methodBuilder.AddCode(
                $"return {namedType.Name.ToFieldName()}Parser.Parse({_objParamName}.Value" +
                $".Get{jsonGetterTypeName}()!);");
        }
    }
}
