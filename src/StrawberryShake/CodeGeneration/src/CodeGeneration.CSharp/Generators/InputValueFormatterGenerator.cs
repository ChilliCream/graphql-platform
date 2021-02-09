using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HotChocolate;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;
using IInputValueFormatter = StrawberryShake.Serialization.IInputValueFormatter;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class InputValueFormatterGenerator : CodeGenerator<NamedTypeDescriptor>
    {
        private static string _keyValuePair =
            TypeNames.KeyValuePair.WithGeneric(TypeNames.String, TypeNames.Object.MakeNullable());

        protected override bool CanHandle(NamedTypeDescriptor descriptor)
        {
            return descriptor.Kind == TypeKind.InputType;
        }

        protected override void Generate(
            CodeWriter writer,
            NamedTypeDescriptor namedTypeDescriptor,
            out string fileName)
        {
            fileName = InputValueFormatterFromType(namedTypeDescriptor);

            NameString typeName = namedTypeDescriptor.Name;
            ClassBuilder classBuilder = ClassBuilder.New()
                .SetName(fileName)
                .AddImplements(TypeNames.IInputValueFormatter);

            var neededSerializers = namedTypeDescriptor.Properties
                .ToLookup(x => x.Type.Name)
                .Select(x => x.First())
                .ToDictionary(x => x.Type.Name);

            ConstructorBuilder constructorBuilder = ConstructorBuilder.New()
                .SetAccessModifier(AccessModifier.Public)
                .SetTypeName(fileName);
            classBuilder.AddConstructor(constructorBuilder);

            var code = CodeBlockBuilder.New();
            constructorBuilder.AddCode(code);

            foreach (var property in neededSerializers.Values)
            {
                var namedType = (NamedTypeDescriptor)property.Type.NamedType();
                var type = InputValueFormatterFromType(namedType);
                var typeWithNamespace = namedType.Kind == TypeKind.LeafType
                    ? TypeNames.StrawberryshakeNamespace + "Serialization." + type
                    : type;
                var parameterName = InputValueFormatterFromType(namedType).WithLowerFirstChar();
                var fieldName = "_" + parameterName;

                ParameterBuilder parameterBuilder = ParameterBuilder.New()
                    .SetType(typeWithNamespace)
                    .SetName(type.WithLowerFirstChar());

                constructorBuilder.AddParameter(parameterBuilder);

                FieldBuilder field = FieldBuilder.New()
                    .SetName(fieldName)
                    .SetAccessModifier(AccessModifier.Private)
                    .SetType(typeWithNamespace)
                    .SetReadOnly();

                classBuilder.AddField(field);

                code.AddCode($"            {fieldName} = {parameterName};\n");
            }

            // TypeName Method
            classBuilder
                .AddProperty("TypeName")
                .SetType(TypeNames.String)
                .AsLambda(namedTypeDescriptor.Name.AsStringToken());

            // Format Method
            var formatBody = new StringBuilder();

            formatBody.AppendLine(@$"
if(!(runtimeValue is {typeName} d)) {{
    throw new {TypeNames.ArgumentException}(nameof(runtimeValue));
}}

return new {_keyValuePair}[] {{");

            for (var index = 0; index < namedTypeDescriptor.Properties.Count; index++)
            {
                PropertyDescriptor? property = namedTypeDescriptor.Properties[index];

                formatBody.Append(
                    $@"    new {_keyValuePair}(
        {property.Name.WithLowerFirstChar().AsStringToken()}, Format{property.Name}(d.{property.Name}))");

                if (index < namedTypeDescriptor.Properties.Count - 1)
                {
                    formatBody.AppendLine(",");
                }
            }

            formatBody.AppendLine("};");

            classBuilder
                .AddMethod(nameof(IInputValueFormatter.Format))
                .SetPublic()
                .SetReturnType(TypeNames.Object.MakeNullable())
                .AddParameter("runtimeValue", x => x.SetType(TypeNames.Object.MakeNullable()))
                .AddCode(CodeBlockBuilder.From(formatBody));

            // generate serialization methods

            foreach (var property in namedTypeDescriptor.Properties)
            {
                classBuilder.AddMethod("Format" + property.Name)
                    .AddParameter("value", x => x.SetType(property.Type.ToBuilder()))
                    .SetReturnType(TypeNames.Object.MakeNullable())
                    .SetPrivate()
                    .AddCode(GenerateSerializer(property.Type, "value"));
            }

            CodeFileBuilder
                .New()
                .SetNamespace(namedTypeDescriptor.Namespace)
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
                case NamedTypeDescriptor descriptor:
                    var type = InputValueFormatterFromType(descriptor);
                    var serializerName = "_" + type.WithLowerFirstChar();
                    var methodCall = $"{serializerName}.Format({variableName})";
                    return assignment == "return"
                        ? CodeLineBuilder.From("return " + methodCall + ";")
                        : CodeLineBuilder.From($"{assignment}.Add({methodCall});");
                case NonNullTypeDescriptor descriptor:
                    return GenerateSerializer(descriptor.InnerType(), variableName, assignment);
                case ListTypeDescriptor descriptor:
                    return CodeBlockBuilder.New()
                        .AddCode(
                            CodeLineBuilder.From(
                                $"var {variableName}_list = new {TypeNames.List.WithGeneric(TypeNames.Object.MakeNullable())}();"))
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
