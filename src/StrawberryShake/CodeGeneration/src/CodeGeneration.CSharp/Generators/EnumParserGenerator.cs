using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.TypeNames;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class EnumParserGenerator : CodeGenerator<EnumTypeDescriptor>
    {
        protected override void Generate(
            CodeWriter writer,
            EnumTypeDescriptor descriptor,
            out string fileName)
        {
            const string serializedValue = nameof(serializedValue);
            const string runtimeValue = nameof(runtimeValue);

            fileName = CreateEnumParserName(descriptor.Name);

            ClassBuilder classBuilder = ClassBuilder
                .New(fileName)
                .AddImplements(IInputValueFormatter)
                .AddImplements(ILeafValueParser.WithGeneric(String, descriptor.Name));

            classBuilder
                .AddMethod("Parse")
                .AddParameter(serializedValue, x => x.SetType(String))
                .SetAccessModifier(AccessModifier.Public)
                .SetReturnType(descriptor.Name)
                .AddCode(CreateEnumParsingSwitch(serializedValue, descriptor));

            classBuilder
                .AddMethod("Format")
                .SetAccessModifier(AccessModifier.Public)
                .SetReturnType(Object)
                .AddParameter(runtimeValue, x => x.SetType(Object.MakeNullable()))
                .AddCode(CreateEnumFormattingSwitch(runtimeValue, descriptor));

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

        private ICode CreateEnumParsingSwitch(
            string serializedValue,
            EnumTypeDescriptor descriptor)
        {
            SwitchExpressionBuilder switchExpression = SwitchExpressionBuilder
                .New()
                .SetReturn()
                .SetExpression(serializedValue)
                .SetDefaultCase(ExceptionBuilder.Inline(TypeNames.GraphQLClientException));

            foreach (var enumValue in descriptor.Values)
            {
                switchExpression.AddCase(
                    enumValue.Name.AsStringToken(),
                    $"{descriptor.Name}.{enumValue.RuntimeValue}");
            }

            return switchExpression;
        }

        private ICode CreateEnumFormattingSwitch(
            string runtimeValue,
            EnumTypeDescriptor descriptor)
        {
            SwitchExpressionBuilder switchExpression =
                SwitchExpressionBuilder.New()
                    .SetReturn()
                    .SetExpression(runtimeValue)
                    .SetDefaultCase(ExceptionBuilder.Inline(TypeNames.GraphQLClientException));

            foreach (var enumValue in descriptor.Values)
            {
                switchExpression.AddCase(
                    $"{descriptor.Name}.{enumValue.RuntimeValue}",
                    enumValue.Name.AsStringToken());
            }

            return switchExpression;
        }
    }
}
