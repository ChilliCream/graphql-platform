using System;
using System.Text;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class EnumValueSerializerGenerator
        : CSharpCodeGenerator<EnumValueSerializerDescriptor>
    {
        protected override Task WriteAsync(
            CodeWriter writer,
            EnumValueSerializerDescriptor descriptor)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            ClassBuilder classBuilder = ClassBuilder.New()
                .SetAccessModifier(AccessModifier.Public)
                .SetSealed()
                .SetName(descriptor.Name)
                .AddImplements(Types.IValueSerializer)
                .AddProperty(PropertyBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .SetType("string")
                    .SetName("Name")
                    .SetGetter(CodeLineBuilder.New()
                        .SetLine($"return \"{descriptor.EnumGraphQLTypeName}\";")))
                .AddProperty(PropertyBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .SetType(Types.ValueKind)
                    .SetName("Kind")
                    .SetGetter(CodeLineBuilder.New()
                        .SetLine($"return {Types.ValueKind}.Enum;")))
                .AddProperty(PropertyBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .SetType(Types.Type)
                    .SetName("ClrType")
                    .SetGetter(CodeLineBuilder.New()
                        .SetLine($"return typeof({descriptor.EnumTypeName});")))
                .AddProperty(PropertyBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .SetType(Types.Type)
                    .SetName("SerializationType")
                    .SetGetter(CodeLineBuilder.New()
                        .SetLine($"return typeof(string);")))
                .AddMethod(MethodBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .SetReturnType("object?", NullableRefTypes)
                    .SetReturnType("object", !NullableRefTypes)
                    .SetName("Serialize")
                    .AddParameter(ParameterBuilder.New()
                        .SetType("object?", NullableRefTypes)
                        .SetType("object", !NullableRefTypes)
                        .SetName("value"))
                    .AddCode(CreateSerializerMethodBody(descriptor, CodeWriter.Indent)))
                .AddMethod(MethodBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .SetReturnType("object?", NullableRefTypes)
                    .SetReturnType("object", !NullableRefTypes)
                    .SetName("Deserialize")
                    .AddParameter(ParameterBuilder.New()
                        .SetType("object?", NullableRefTypes)
                        .SetType("object", !NullableRefTypes)
                        .SetName("serialized"))
                    .AddCode(CreateDeserializerMethodBody(descriptor, CodeWriter.Indent)));

            return CodeFileBuilder.New()
                .SetNamespace(descriptor.Namespace)
                .AddType(classBuilder)
                .BuildAsync(writer);
        }

        private CodeBlockBuilder CreateSerializerMethodBody(
            EnumValueSerializerDescriptor descriptor,
            string indent)
        {
            var code = new StringBuilder();

            code.AppendLine("if (value is null)");
            code.AppendLine("{");
            code.AppendLine($"{indent}return null;");
            code.AppendLine("}");
            code.AppendLine();

            code.AppendLine($"var enumValue = ({descriptor.EnumTypeName})value");
            code.AppendLine();

            code.AppendLine("switch(enumValue)");
            code.AppendLine("{");

            foreach (EnumElementDescriptor element in descriptor.Elements)
            {
                code.AppendLine($"{indent}case {descriptor.EnumTypeName}.{element.Name}:");
                code.AppendLine($"{indent}{indent}return \"{element.SerializedName}\";");
            }

            code.AppendLine($"{indent}default:");
            code.AppendLine($"{indent}{indent}throw new {Types.NotSupportedException}();");

            code.AppendLine("}");

            return CodeBlockBuilder.FromStringBuilder(code);
        }

        private CodeBlockBuilder CreateDeserializerMethodBody(
            EnumValueSerializerDescriptor descriptor,
            string indent)
        {
            var code = new StringBuilder();

            code.AppendLine("if (serialized is null)");
            code.AppendLine("{");
            code.AppendLine($"{indent}return null;");
            code.AppendLine("}");
            code.AppendLine();

            code.AppendLine($"var stringValue = (string)serialized");
            code.AppendLine();

            code.AppendLine("switch(serialized)");
            code.AppendLine("{");

            foreach (EnumElementDescriptor element in descriptor.Elements)
            {
                code.AppendLine(
                    $"{indent}case \"{element.SerializedName}\":");
                code.AppendLine(
                    $"{indent}{indent}return {descriptor.EnumTypeName}.{element.Name};");
            }

            code.AppendLine($"{indent}default:");
            code.AppendLine($"{indent}{indent}throw new {Types.NotSupportedException}();");

            code.AppendLine("}");

            return CodeBlockBuilder.FromStringBuilder(code);
        }
    }
}
