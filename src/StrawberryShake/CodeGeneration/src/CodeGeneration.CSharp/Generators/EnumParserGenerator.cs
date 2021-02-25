using System.Text;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using static StrawberryShake.CodeGeneration.NamingConventions;
using static StrawberryShake.CodeGeneration.TypeNames;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class EnumParserGenerator : CodeGenerator<EnumTypeDescriptor>
    {
        protected override void Generate(
            CodeWriter writer,
            EnumTypeDescriptor descriptor,
            out string fileName)
        {
            fileName = CreateEnumParserName(descriptor.Name);

            ClassBuilder classBuilder = ClassBuilder
                .New(fileName)
                .AddImplements(IInputValueFormatter)
                .AddImplements(ILeafValueParser.WithGeneric(String, descriptor.Name));

            const string serializedValueParamName = "serializedValue";

            classBuilder
                .AddMethod("Parse")
                .AddParameter(ParameterBuilder.New()
                    .SetName(serializedValueParamName)
                    .SetType(String))
                .SetAccessModifier(AccessModifier.Public)
                .SetReturnType(descriptor.Name)
                .AddCode(CreateEnumParsingSwitch(serializedValueParamName, descriptor));

            const string runtimeValueParamName = "runtimeValue";
            
            classBuilder
                .AddMethod("Format")
                .SetAccessModifier(AccessModifier.Public)
                .SetReturnType("object")
                .AddParameter(ParameterBuilder.New()
                    .SetType(Object.MakeNullable())
                    .SetName(runtimeValueParamName))
                .AddCode(CreateEnumFormattingSwitch(runtimeValueParamName, descriptor));

            classBuilder
                .AddProperty("TypeName")
                .AsLambda(descriptor.Name.AsStringToken())
                .SetPublic()
                .SetType(String);

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(classBuilder)
                .Build(writer);
        }

        private CodeBlockBuilder CreateEnumParsingSwitch(
            string paramName,
            EnumTypeDescriptor descriptor)
        {
            StringBuilder sourceText = new StringBuilder()
                .AppendLine($"return {paramName} switch")
                .AppendLine("{")
                .AppendLineForEach(
                    descriptor.Values,
                    x => $"    \"{x.Name}\" => {descriptor.Name}.{x.RuntimeValue},")
                .AppendLine($"    _ => throw new {TypeNames.GraphQLClientException}()")
                .AppendLine("};");

            return CodeBlockBuilder.From(sourceText);
        }

        private CodeBlockBuilder CreateEnumFormattingSwitch(
            string paramName,
            EnumTypeDescriptor descriptor)
        {
            var sourceText = new StringBuilder();

            sourceText.AppendLine($"return {paramName} switch");
            sourceText.AppendLine("{");

            foreach (var enumValue in descriptor.Values)
            {
                sourceText.AppendLine(
                    $"    {descriptor.Name}.{enumValue.RuntimeValue} => \"{enumValue.Name}\",");
            }

            sourceText.AppendLine($"    _ => throw new {TypeNames.GraphQLClientException}()");
            sourceText.AppendLine("};");

            return CodeBlockBuilder.From(sourceText);
        }
    }
}
