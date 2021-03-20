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

        protected override bool CanHandle(InputObjectTypeDescriptor descriptor)
        {
            return true;
        }

        protected override void Generate(
            CodeWriter writer,
            InputObjectTypeDescriptor namedTypeDescriptor,
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
            string variableName,
            string assignment = "return")
        {
            RuntimeTypeInfo runtimeType = typeDescriptor.GetRuntimeType();
            var isValueType = runtimeType.IsValueType;

            switch (typeDescriptor)
            {
                case INamedTypeDescriptor descriptor:
                    var serializerName = GetFieldName(descriptor.GetName().Value) + "Formatter";
                    MethodCallBuilder methodCall = MethodCallBuilder
                        .New()
                        .SetMethodName(serializerName, "Format")
                        .AddArgument(variableName);

                    return assignment == "return"
                        ? methodCall.SetReturn()
                        : MethodCallBuilder
                            .New()
                            .SetMethodName(assignment, nameof(List<object>.Add))
                            .AddArgument(methodCall.SetDetermineStatement(false));

                case NonNullTypeDescriptor descriptor:
                    return CodeBlockBuilder
                        .New()
                        .If(!isValueType,
                            i =>
                            {
                                i.AddIf(x => x
                                        .SetCondition($"{variableName} is null")
                                        .AddCode(
                                            ExceptionBuilder
                                                .New(TypeNames.ArgumentNullException)
                                                .AddArgument($"nameof({variableName})")))
                                    .AddEmptyLine();
                            })
                        .AddCode(
                            GenerateSerializer(
                                descriptor.InnerType(),
                                variableName,
                                assignment));

                case ListTypeDescriptor descriptor:
                    return CodeBlockBuilder
                        .New()
                        .AddCode(
                            AssignmentBuilder
                                .New()
                                .SetLefthandSide($"var {variableName}_list")
                                .SetRighthandSide(
                                    MethodCallBuilder
                                        .Inline()
                                        .SetNew()
                                        .SetMethodName(TypeNames.List)
                                        .AddGeneric(TypeNames.Object.MakeNullable())))
                        .AddEmptyLine()
                        .AddCode(
                            ForEachBuilder
                                .New()
                                .SetLoopHeader($"var {variableName}_elm in {variableName}")
                                .AddCode(
                                    GenerateSerializer(
                                        descriptor.InnerType(),
                                        variableName + "_elm",
                                        variableName + "_list")))
                        .AddCode(
                            assignment == "return"
                                ? CodeLineBuilder.From($"return {variableName}_list;")
                                : CodeLineBuilder.From(
                                    $"{assignment}.Add({variableName}_list);"));
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
