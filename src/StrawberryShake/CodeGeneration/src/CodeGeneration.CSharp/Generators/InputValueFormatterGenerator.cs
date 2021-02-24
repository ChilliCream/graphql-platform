using System;
using System.Linq;
using HotChocolate;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;
using StrawberryShake.Serialization;
using IInputValueFormatter = StrawberryShake.Serialization.IInputValueFormatter;
using static StrawberryShake.CodeGeneration.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class InputValueFormatterGenerator : CodeGenerator<InputObjectTypeDescriptor>
    {
        private static readonly string _keyValuePair =
            TypeNames.KeyValuePair.WithGeneric(
                TypeNames.String,
                TypeNames.Object.MakeNullable());

        protected override bool CanHandle(InputObjectTypeDescriptor descriptor)
        {
            return true;
        }

        protected override void Generate(
            CodeWriter writer,
            InputObjectTypeDescriptor namedTypeDescriptor,
            out string fileName)
        {
            fileName = CreateInputValueFormatter(namedTypeDescriptor);

            NameString typeName = namedTypeDescriptor.Name;
            ClassBuilder classBuilder = ClassBuilder.New()
                .SetName(fileName)
                .AddImplements(TypeNames.IInputValueFormatter);

            var neededSerializers = namedTypeDescriptor.Properties
                .ToLookup(x => x.Type.Name)
                .Select(x => x.First())
                .ToDictionary(x => x.Type.Name);

            //  Initialize Method

            CodeBlockBuilder initialize = classBuilder
                .AddMethod("Initialize")
                .SetPublic()
                .AddParameter("serializerResolver", x => x.SetType(TypeNames.ISerializerResolver))
                .AddBody();

            foreach (var property in neededSerializers.Values)
            {
                if (property.Type.GetGraphQlTypeName()?.Value is { } name)
                {
                    MethodCallBuilder call = MethodCallBuilder.New()
                        .SetMethodName("serializerResolver." +
                            nameof(ISerializerResolver.GetInputValueFormatter))
                        .AddArgument(name.AsStringToken() ?? "")
                        .SetPrefix($"{GetFieldName(name)}Formatter = ");

                    initialize.AddCode(call);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Serializer for property {namedTypeDescriptor.Name}.{property.Name} " +
                        $"could not be created. GraphQLTypeName was empty");
                }
            }

            // Serializer Fields

            foreach (var property in neededSerializers.Values)
            {
                if (property.Type.GetGraphQlTypeName()?.Value is { } name)
                {
                    FieldBuilder field = FieldBuilder.New()
                        .SetName(GetFieldName(name) + "Formatter")
                        .SetAccessModifier(AccessModifier.Private)
                        .SetType(TypeNames.IInputValueFormatter)
                        .SetValue("default!");

                    classBuilder.AddField(field);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Serializer for property {namedTypeDescriptor.Name}.{property.Name} " +
                        $"could not be created. GraphQLTypeName was empty");
                }
            }

            // TypeName Method

            classBuilder
                .AddProperty("TypeName")
                .SetType(TypeNames.String)
                .AsLambda(namedTypeDescriptor.Name.AsStringToken());

            // Format Method

            classBuilder
                .AddMethod(nameof(IInputValueFormatter.Format))
                .SetPublic()
                .SetReturnType(TypeNames.Object.MakeNullable())
                .AddParameter(
                    "runtimeValue",
                    x => x.SetType(TypeNames.Object.MakeNullable()))
                .AddBody()
                .ArgumentException("runtimeValue", $"!(runtimeValue is {typeName} d)")
                .AddEmptyLine()
                .AddArray()
                .SetPrefix("return ")
                .ForEach(namedTypeDescriptor.Properties,
                    (builder, property) =>
                        builder
                            .SetType(_keyValuePair)
                            .AddAssigment(MethodCallBuilder.New()
                                .SetPrefix("new ")
                                .SetDetermineStatement(false)
                                .SetMethodName(_keyValuePair)
                                .AddArgument(GetParameterName(property.Name).AsStringToken())
                                .AddArgument(MethodCallBuilder.New()
                                    .SetMethodName($"Format{property.Name}")
                                    .SetDetermineStatement(false)
                                    .AddArgument($"d.{property.Name}"))));

            // Serializer Methods

            classBuilder.ForEach(
                namedTypeDescriptor.Properties,
                (builder, property) =>
                    builder.AddMethod("Format" + property.Name)
                        .AddParameter("value", x => x.SetType(property.Type.ToBuilder()))
                        .SetReturnType(TypeNames.Object.MakeNullable())
                        .SetPrivate()
                        .AddCode(GenerateSerializer(property.Type, "value")));

            CodeFileBuilder
                .New()
                .SetNamespace(namedTypeDescriptor.RuntimeType.Namespace)
                .AddType(classBuilder)
                .Build(writer);
        }

        public static ICode GenerateSerializer(
            ITypeDescriptor typeDescriptor,
            string variableName,
            string assignment = "return")
        {
            switch (typeDescriptor)
            {
                case INamedTypeDescriptor descriptor:
                    var type = descriptor.GetGraphQlTypeName()?.Value + "Formatter";
                    var serializerName = GetFieldName(type);
                    var methodCall = $"{serializerName}.Format({variableName})";
                    return assignment == "return"
                        ? CodeLineBuilder.From("return " + methodCall + ";")
                        : CodeLineBuilder.From($"{assignment}.Add({methodCall});");
                case NonNullTypeDescriptor descriptor:
                    return CodeBlockBuilder.New()
                        .AddIf(x =>
                            x.SetCondition($"{variableName} == default")
                                .AddCode(
                                    assignment == "return"
                                        ? CodeLineBuilder.From("return null;")
                                        : CodeBlockBuilder.New()
                                            .AddLine($"{assignment}.Add(null);")
                                            .AddLine("continue;")))
                        .AddEmptyLine()
                        .AddCode(GenerateSerializer(descriptor.InnerType(),
                            variableName,
                            assignment));
                case ListTypeDescriptor descriptor:
                    return CodeBlockBuilder.New()
                        .AddCode(
                            CodeLineBuilder.From(
                                $"var {variableName}_list = new " +
                                $"{TypeNames.List.WithGeneric(TypeNames.Object.MakeNullable())}" +
                                "();"))
                        .AddEmptyLine()
                        .AddCode(
                            ForEachBuilder.New()
                                .SetLoopHeader($"var {variableName}_elm in {variableName}")
                                .AddCode(
                                    GenerateSerializer(
                                        descriptor.InnerType(),
                                        variableName + "_elm",
                                        variableName + "_list")))
                        .AddCode(
                            assignment == "return"
                                ? CodeLineBuilder.From($"return {variableName}_list;")
                                : CodeLineBuilder.From($"{assignment}.Add({variableName}_list);"));
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
