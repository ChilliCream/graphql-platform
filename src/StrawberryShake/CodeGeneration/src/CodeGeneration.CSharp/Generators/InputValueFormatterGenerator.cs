using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public class InputValueFormatterGenerator : CodeGenerator<InputObjectTypeDescriptor>
{
    private static readonly string _keyValuePair =
        TypeNames.KeyValuePair.WithGeneric(
            TypeNames.String,
            TypeNames.Object.MakeNullable());

    protected override void Generate(
        InputObjectTypeDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings,
        CodeWriter writer,
        out string fileName,
        out string? path,
        out string ns)
    {
        const string serializerResolver = nameof(serializerResolver);
        const string runtimeValue = nameof(runtimeValue);
        const string input = nameof(input);
        const string inputInfo = nameof(inputInfo);
        const string fields = nameof(fields);

        fileName = CreateInputValueFormatter(descriptor);
        path = Serialization;
        ns = descriptor.RuntimeType.NamespaceWithoutGlobal;

        var stateNamespace = $"{descriptor.RuntimeType.Namespace}.{State}";
        var infoInterfaceType = $"{stateNamespace}.{CreateInputValueInfo(descriptor.Name)}";

        var classBuilder = ClassBuilder
            .New()
            .SetAccessModifier(settings.AccessModifier)
            .SetName(fileName)
            .AddImplements(TypeNames.IInputObjectFormatter);

        var neededSerializers = descriptor
            .Properties
            .GroupBy(x => x.Type.Name)
            .ToDictionary(x => x, x => x.First());

        //  Initialize Method

        var initialize = classBuilder
            .AddMethod("Initialize")
            .SetPublic()
            .AddParameter(serializerResolver, x => x.SetType(TypeNames.ISerializerResolver))
            .AddBody();

        foreach (var property in neededSerializers.Values)
        {
            if (property.Type.GetName() is { } name)
            {
                var propertyName = GetFieldName(name) + "Formatter";

                initialize
                    .AddAssignment(propertyName)
                    .AddMethodCall()
                    .SetMethodName(serializerResolver, "GetInputValueFormatter")
                    .AddArgument(name.AsStringToken());

                classBuilder
                    .AddField(propertyName)
                    .SetAccessModifier(AccessModifier.Private)
                    .SetType(TypeNames.IInputValueFormatter)
                    .SetValue("default!");
            }
            else
            {
                throw new InvalidOperationException(
                    $"Serializer for property {descriptor.Name}.{property.Name} " +
                    "could not be created. GraphQLTypeName was empty");
            }
        }

        // TypeName Method

        classBuilder
            .AddProperty("TypeName")
            .SetType(TypeNames.String)
            .AsLambda(descriptor.Name.AsStringToken());

        // Format Method

        var codeBlock =
            classBuilder
                .AddMethod("Format")
                .SetPublic()
                .SetReturnType(TypeNames.Object.MakeNullable())
                .AddParameter(runtimeValue, x => x.SetType(TypeNames.Object.MakeNullable()))
                .AddBody()
                .AddCode(IfBuilder
                    .New()
                    .SetCondition($"{runtimeValue} is null")
                    .AddCode("return null;"))
                .AddEmptyLine()
                .AddCode(AssignmentBuilder
                    .New()
                    .SetLeftHandSide($"var {input}")
                    .SetRightHandSide($"{runtimeValue} as {descriptor.RuntimeType}"))
                .AddCode(AssignmentBuilder
                    .New()
                    .SetLeftHandSide($"var {inputInfo}")
                    .SetRightHandSide($"{runtimeValue} as {infoInterfaceType}"))
                .ArgumentException(runtimeValue, $"{input} is null || {inputInfo} is null")
                .AddEmptyLine();

        codeBlock
            .AddAssignment($"var {fields}")
            .SetRightHandSide($"new {TypeNames.List.WithGeneric(_keyValuePair)}()");

        codeBlock.AddEmptyLine();

        foreach (var property in descriptor.Properties)
        {
            var serializerMethodName = $"Format{property.Name}";

            codeBlock.AddCode(
                SerializeField(property, input, inputInfo, fields, serializerMethodName));

            classBuilder
                .AddMethod(serializerMethodName)
                .AddParameter(input, x => x.SetType(property.Type.ToTypeReference()))
                .SetReturnType(TypeNames.Object.MakeNullable())
                .SetPrivate()
                .AddCode(GenerateSerializer(property.Type, input));
        }

        codeBlock.AddCode($"return {fields};");

        classBuilder.Build(writer);
    }

    private IfBuilder SerializeField(
        PropertyDescriptor property,
        string input,
        string inputInfo,
        string fields,
        string serializerMethod) =>
        IfBuilder
            .New()
            .SetCondition($"{inputInfo}.{CreateIsSetProperty(property.Name)}")
            .AddCode(
                MethodCallBuilder
                    .New()
                    .SetMethodName($"{fields}.Add")
                    .AddArgument(
                        MethodCallBuilder
                            .Inline()
                            .SetNew()
                            .SetMethodName(_keyValuePair)
                            .AddArgument(property.FieldName.AsStringToken())
                            .AddArgument(MethodCallBuilder
                                .Inline()
                                .SetMethodName(serializerMethod)
                                .AddArgument($"{input}.{property.Name}"))));

    public static ICode GenerateSerializer(
        ITypeDescriptor typeDescriptor,
        string variableName)
    {
        const string @return = "return";

        return GenerateSerializerLocal(typeDescriptor, variableName, @return, true);

        ICode GenerateSerializerLocal(
            ITypeDescriptor currentType,
            string variable,
            string assignment,
            bool isNullable)
        {
            var runtimeType = currentType.GetRuntimeType();
            var isValueType = runtimeType.IsValueType;

            ICode format = currentType switch
            {
                INamedTypeDescriptor d when assignment == @return =>
                    BuildFormatterMethodCall(variable, d).SetReturn(),

                INamedTypeDescriptor d =>
                    MethodCallBuilder
                        .New()
                        .SetMethodName(assignment, nameof(List<object>.Add))
                        .AddArgument(
                            BuildFormatterMethodCall(variable, d).SetDetermineStatement(false)),

                NonNullTypeDescriptor d when !isValueType =>
                    CodeBlockBuilder
                        .New()
                        .AddIf(x => x
                            .SetCondition($"{variable} is null")
                            .AddCode(ExceptionBuilder
                                .New(TypeNames.ArgumentNullException)
                                .AddArgument($"nameof({variable})")))
                        .AddEmptyLine()
                        .AddCode(
                            GenerateSerializerLocal(
                                d.InnerType(),
                                variable,
                                assignment,
                                false)),

                NonNullTypeDescriptor d =>
                    CodeBlockBuilder
                        .New()
                        .AddCode(
                            GenerateSerializerLocal(
                                d.InnerType(),
                                variable,
                                assignment,
                                false)),

                ListTypeDescriptor d =>
                    CodeBlockBuilder
                        .New()
                        .AddCode(AssignmentBuilder
                            .New()
                            .SetLeftHandSide($"var {variable}_list")
                            .SetRightHandSide(MethodCallBuilder.Inline()
                                .SetNew()
                                .SetMethodName(TypeNames.List)
                                .AddGeneric(TypeNames.Object.MakeNullable())))
                        .AddEmptyLine()
                        .AddCode(ForEachBuilder
                            .New()
                            .SetLoopHeader(
                                $"var {variable}_elm in {variable}")
                            .AddCode(
                                GenerateSerializerLocal(
                                    d.InnerType(),
                                    variable + "_elm",
                                    variable + "_list",
                                    true)))
                        .AddCode(CodeLineBuilder
                            .From(
                                assignment == @return
                                    ? $"return {variable}_list;"
                                    : $"{assignment}.Add({variable}_list);")),
                _ => throw new InvalidOperationException(),
            };

            if (isNullable && currentType is not NonNullTypeDescriptor)
            {
                return IfBuilder
                    .New()
                    .SetCondition($"{variable} is null")
                    .AddCode(CodeLineBuilder
                        .From(assignment == @return
                            ? $"return {variable};"
                            : $"{assignment}.Add({variable});"))
                    .AddElse(format);
            }

            return format;
        }
    }

    private static MethodCallBuilder BuildFormatterMethodCall(
        string variableName,
        INamedTypeDescriptor descriptor)
    {
        return MethodCallBuilder
            .New()
            .SetMethodName(GetFieldName(descriptor.GetName()) + "Formatter", "Format")
            .AddArgument(variableName);
    }
}
