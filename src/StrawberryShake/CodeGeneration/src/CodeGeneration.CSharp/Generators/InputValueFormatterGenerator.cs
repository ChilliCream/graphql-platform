using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class InputValueFormatterGenerator : CodeGenerator<InputObjectTypeDescriptor>
    {
        private static readonly string _keyValuePair =
            TypeNames.KeyValuePair.WithGeneric(TypeNames.String, TypeNames.Object.MakeNullable());

        protected override void Generate(InputObjectTypeDescriptor namedTypeDescriptor,
            CodeGeneratorSettings settings,
            CodeWriter writer,
            out string fileName,
            out string? path)
        {
            const string serializerResolver = nameof(serializerResolver);
            const string runtimeValue = nameof(runtimeValue);
            const string value = nameof(value);

            fileName = CreateInputValueFormatter(namedTypeDescriptor);
            path = Serialization;

            NameString typeName = namedTypeDescriptor.Name;

            ClassBuilder classBuilder = ClassBuilder
                .New()
                .SetName(fileName)
                .AddImplements(TypeNames.IInputObjectFormatter);

            var neededSerializers = namedTypeDescriptor
                .Properties
                .GroupBy(x => x.Type.Name)
                .ToDictionary(x => x, x => x.First());

            //  Initialize Method

            CodeBlockBuilder initialize = classBuilder
                .AddMethod("Initialize")
                .SetPublic()
                .AddParameter(serializerResolver, x => x.SetType(TypeNames.ISerializerResolver))
                .AddBody();

            foreach (var property in neededSerializers.Values)
            {
                if (property.Type.GetName().Value is { } name)
                {
                    var propertyName = GetFieldName(name) + "Formatter";

                    initialize
                        .AddAssigment(propertyName)
                        .AddMethodCall()
                        .SetMethodName(
                            serializerResolver,
                            "GetInputValueFormatter")
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
                        $"Serializer for property {namedTypeDescriptor.Name}.{property.Name} " +
                        "could not be created. GraphQLTypeName was empty");
                }
            }

            // TypeName Method

            classBuilder
                .AddProperty("TypeName")
                .SetType(TypeNames.String)
                .AsLambda(namedTypeDescriptor.Name.AsStringToken());

            // Format Method

            ArrayBuilder arrayBuilder = classBuilder
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
                .ArgumentException(runtimeValue, $"!({runtimeValue} is {typeName} d)")
                .AddEmptyLine()
                .AddArray()
                .SetReturn();

            foreach (var property in namedTypeDescriptor.Properties)
            {
                var serializerMethodName = $"Format{property.Name}";

                arrayBuilder
                    .SetType(_keyValuePair)
                    .AddAssigment(MethodCallBuilder
                        .Inline()
                        .SetNew()
                        .SetMethodName(_keyValuePair)
                        .AddArgument(property.FieldName.AsStringToken())
                        .AddArgument(MethodCallBuilder
                            .Inline()
                            .SetMethodName(serializerMethodName)
                            .AddArgument($"d.{property.Name}")));

                classBuilder
                    .AddMethod(serializerMethodName)
                    .AddParameter(value, x => x.SetType(property.Type.ToTypeReference()))
                    .SetReturnType(TypeNames.Object.MakeNullable())
                    .SetPrivate()
                    .AddCode(GenerateSerializer(property.Type, value));
            }

            CodeFileBuilder
                .New()
                .SetNamespace(namedTypeDescriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(classBuilder)
                .Build(writer);
        }

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
                RuntimeTypeInfo runtimeType = currentType.GetRuntimeType();
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
                                .SetLefthandSide($"var {variable}_list")
                                .SetRighthandSide(MethodCallBuilder.Inline()
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
                    _ => throw new InvalidOperationException()
                };

                if (isNullable && currentType is not NonNullTypeDescriptor)
                {
                    return IfBuilder
                        .New()
                        .SetCondition($"!({variable} is null)")
                        .AddCode(format)
                        .AddElse(CodeLineBuilder
                            .From(
                                assignment == @return
                                    ? $"return {variable};"
                                    : $"{assignment}.Add({variable});"));
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
                .SetMethodName(GetFieldName(descriptor.GetName().Value) + "Formatter", "Format")
                .AddArgument(variableName);
        }
    }
}
