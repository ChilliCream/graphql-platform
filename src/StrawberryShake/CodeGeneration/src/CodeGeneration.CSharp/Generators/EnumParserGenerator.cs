using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public class EnumParserGenerator : CodeGenerator<EnumTypeDescriptor>
{
    protected override void Generate(EnumTypeDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings,
        CodeWriter writer,
        out string fileName,
        out string? path,
        out string ns)
    {
        const string serializedValue = nameof(serializedValue);
        const string runtimeValue = nameof(runtimeValue);

        fileName = CreateEnumParserName(descriptor.Name);
        path = Serialization;
        ns = descriptor.RuntimeType.NamespaceWithoutGlobal;

        var classBuilder = ClassBuilder
            .New(fileName)
            .SetAccessModifier(settings.AccessModifier)
            .AddImplements(TypeNames.IInputValueFormatter)
            .AddImplements(
                TypeNames.ILeafValueParser.WithGeneric(TypeNames.String, descriptor.Name));

        classBuilder
            .AddMethod("Parse")
            .AddParameter(serializedValue, x => x.SetType(TypeNames.String))
            .SetAccessModifier(AccessModifier.Public)
            .SetReturnType(descriptor.Name)
            .AddCode(CreateEnumParsingSwitch(serializedValue, descriptor));

        classBuilder
            .AddMethod("Format")
            .SetAccessModifier(AccessModifier.Public)
            .SetReturnType(TypeNames.Object)
            .AddParameter(runtimeValue, x => x.SetType(TypeNames.Object.MakeNullable()))
            .AddCode(CreateEnumFormattingSwitch(runtimeValue, descriptor));

        classBuilder
            .AddProperty("TypeName")
            .AsLambda(descriptor.Name.AsStringToken())
            .SetPublic()
            .SetType(TypeNames.String);

        classBuilder.Build(writer);
    }

    private ICode CreateEnumParsingSwitch(
        string serializedValue,
        EnumTypeDescriptor descriptor)
    {
        var switchExpression = SwitchExpressionBuilder
            .New()
            .SetReturn()
            .SetExpression(serializedValue)
            .SetDefaultCase(ExceptionBuilder.Inline(TypeNames.GraphQLClientException)
                .AddArgument($"$\"String value '{{serializedValue}}' can't be converted to enum {descriptor.Name}\""));

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
        var switchExpression =
            SwitchExpressionBuilder.New()
                .SetReturn()
                .SetExpression(runtimeValue)
                .SetDefaultCase(ExceptionBuilder.Inline(TypeNames.GraphQLClientException)
                    .AddArgument($"$\"Enum {descriptor.Name} value '{{runtimeValue}}' can't be converted to string\""));

        foreach (var enumValue in descriptor.Values)
        {
            switchExpression.AddCase(
                $"{descriptor.Name}.{enumValue.RuntimeValue}",
                enumValue.Name.AsStringToken());
        }

        return switchExpression;
    }
}
