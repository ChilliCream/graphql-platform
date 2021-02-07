using System.Text;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class EnumParserGenerator : CodeGenerator<EnumDescriptor>
    {
        protected override void Generate(
            CodeWriter writer,
            EnumDescriptor descriptor,
            out string fileName)
        {
            fileName = NamingConventions.EnumParserNameFromEnumName(descriptor.Name);
            var classBuilder = ClassBuilder.New()
                .SetName(fileName);


            var serializedValueParamName = "serializedValue";
            var parseMethod = MethodBuilder.New()
                .AddParameter(ParameterBuilder.New()
                    .SetName(serializedValueParamName)
                    .SetType(TypeNames.String))
                .SetName("Parse")
                .SetReturnType(descriptor.Name)
                .AddCode(CreateEnumParsingSwitch(serializedValueParamName, descriptor));
            classBuilder.AddMethod(parseMethod);

            var runtimeValueParamName = "runtimeValue";
            var formatMethod = MethodBuilder.New()
                .SetName("Format")
                .SetAccessModifier(AccessModifier.Public)
                .SetReturnType("object")
                .AddParameter(ParameterBuilder.New()
                    .SetType("object")
                    .SetName(runtimeValueParamName))
                .AddCode(CreateEnumFormatingSwitch(runtimeValueParamName, descriptor));
            classBuilder.AddMethod(formatMethod);

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.Namespace)
                .AddType(classBuilder)
                .Build(writer);
        }

        private CodeBlockBuilder CreateEnumParsingSwitch(
            string paramName,
            EnumDescriptor descriptor)
        {
            var sourceText = new StringBuilder();

            sourceText.AppendLine($"return {paramName} switch");
            sourceText.AppendLine("{");

            foreach (var enumValue in descriptor.Values)
            {
                sourceText.AppendLine(
                    $"    \"{enumValue.GraphQLName}\" => {descriptor.Name}.{enumValue.Name},");
            }

            sourceText.AppendLine($"    _ => throw new {TypeNames.IGraphQLClientException}()");
            sourceText.AppendLine("};");

            return CodeBlockBuilder.From(sourceText);
        }

        private CodeBlockBuilder CreateEnumFormatingSwitch(
            string paramName,
            EnumDescriptor descriptor)
        {
            var sourceText = new StringBuilder();

            sourceText.AppendLine($"return {paramName} switch");
            sourceText.AppendLine("{");

            foreach (var enumValue in descriptor.Values)
            {
                sourceText.AppendLine(
                    $"    {descriptor.Name}.{enumValue.Name} => \"{enumValue.GraphQLName}\",");
            }

            sourceText.AppendLine($"    _ => throw new {TypeNames.IGraphQLClientException}()");
            sourceText.AppendLine("};");

            return CodeBlockBuilder.From(sourceText);
        }
    }
}
