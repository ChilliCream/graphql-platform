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

            ClassBuilder classBuilder = ClassBuilder
                .New(fileName)
                .AddImplements(TypeNames.IInputValueFormatter)
                .AddImplements(
                    TypeNames.ILeafValueParser.WithGeneric(TypeNames.String, descriptor.Name));

            const string serializedValueParamName = "serializedValue";
            classBuilder
                .AddMethod("Parse")
                .AddParameter(ParameterBuilder.New()
                    .SetName(serializedValueParamName)
                    .SetType(TypeNames.String))
                .SetAccessModifier(AccessModifier.Public)
                .SetReturnType(descriptor.Name)
                .AddCode(CreateEnumParsingSwitch(serializedValueParamName, descriptor));

            const string runtimeValueParamName = "runtimeValue";
            classBuilder
                .AddMethod("Format")
                .SetAccessModifier(AccessModifier.Public)
                .SetReturnType("object")
                .AddParameter(ParameterBuilder.New()
                    .SetType(TypeNames.Object.MakeNullable())
                    .SetName(runtimeValueParamName))
                .AddCode(CreateEnumFormatingSwitch(runtimeValueParamName, descriptor));

            classBuilder
                .AddProperty("TypeName")
                .AsLambda(descriptor.Name.AsStringToken())
                .SetPublic()
                .SetType(TypeNames.String);

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
            StringBuilder sourceText = new StringBuilder()
                .AppendLine($"return {paramName} switch")
                .AppendLine("{")
                .AppendLineForEach(
                    descriptor.Values,
                    x => $"    \"{x.GraphQLName}\" => {descriptor.Name}.{x.Name},")
                .AppendLine($"    _ => throw new {TypeNames.GraphQLClientException}()")
                .AppendLine("};");

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

            sourceText.AppendLine($"    _ => throw new {TypeNames.GraphQLClientException}()");
            sourceText.AppendLine("};");

            return CodeBlockBuilder.From(sourceText);
        }
    }
}
