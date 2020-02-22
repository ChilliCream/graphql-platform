using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultParserGenerator
        : CSharpCodeGenerator<ResultParserDescriptor>
    {
        protected override Task WriteAsync(
            CodeWriter writer,
            ResultParserDescriptor descriptor)
        {
            ClassBuilder classBuilder =
                ClassBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .SetName(descriptor.Name)
                    .AddImplements($"{Types.JsonResultParserBase}<{descriptor.ResultType}>");

            AddConstructor(
                classBuilder,
                descriptor.ValueSerializers,
                CodeWriter.Indent);

            foreach (ResultParserMethodDescriptor parserMethod in descriptor.ParseMethods)
            {

            }

            return CodeFileBuilder.New()
                .SetNamespace(descriptor.Namespace)
                .AddType(classBuilder)
                .BuildAsync(writer);
        }

        private void AddConstructor(
            ClassBuilder classBuilder,
            IReadOnlyList<ValueSerializerDescriptor> serializers,
            string indent)
        {
            foreach (ValueSerializerDescriptor serializer in serializers)
            {
                classBuilder.AddField(
                    FieldBuilder.New()
                        .SetType(Types.ValueSerializer)
                        .SetName(serializer.FieldName));
            }

            classBuilder.AddConstructor(
                ConstructorBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .AddParameter(ParameterBuilder.New()
                        .SetType(Types.ValueSerializerCollection)
                        .SetName("serializerResolver"))
                    .AddCode(CreateConstructorBody(serializers, indent)));
        }

        private CodeBlockBuilder CreateConstructorBody(
            IReadOnlyList<ValueSerializerDescriptor> serializers,
            string indent)
        {
            var body = new StringBuilder();

            body.AppendLine(
                $"public OnReviewResultParser({Types.ValueSerializerCollection} " +
                "serializerResolver)");
            body.AppendLine("if (serializerResolver is null)");
            body.AppendLine("{");
            body.AppendLine(
                $"{indent}throw new {Types.ArgumentNullException}" +
                "(nameof(serializerResolver));");
            body.AppendLine("}");
            body.AppendLine();

            for (int i = 0; i < serializers.Count; i++)
            {
                if (i > 0)
                {
                    body.AppendLine();
                }
                body.Append(
                    $"{serializers[i].FieldName} = serializerResolver." +
                    $"Get(\"{serializers[i].Name}\");");
            }

            return CodeBlockBuilder.FromStringBuilder(body);
        }

        private void AddParseDataMethod(
            ClassBuilder classBuilder,
            ResultParserMethodDescriptor methodDescriptor,
            string indent)
        {
            classBuilder.AddMethod(
                MethodBuilder.New()
                    .SetAccessModifier(AccessModifier.Protected)
                    .SetInheritance(Inheritance.Override)
                    .SetReturnType($"{methodDescriptor.ResultType}?", IsNullable(methodDescriptor))
                    .SetReturnType($"{methodDescriptor.ResultType}?", !IsNullable(methodDescriptor))
                    .SetName("ParseData")
                    .AddParameter(ParameterBuilder.New()
                        .SetType(Types.JsonElement)
                        .SetName("data"))
                    .AddCode(CreateParseDataMethodBody(
                        classBuilder, methodDescriptor, indent)));
        }

        private CodeBlockBuilder CreateParseDataMethodBody(
            ClassBuilder classBuilder,
            ResultParserMethodDescriptor methodDescriptor,
            string indent)
        {
            var body = new StringBuilder();

            body.AppendLine($"return new {methodDescriptor.ResultModelType}");
            body.AppendLine("(");

            for (int i = 0; i < methodDescriptor.Fields.Count; i++)
            {
                ResultFieldDescriptor field = methodDescriptor.Fields[i];

                if (i > 0)
                {
                    body.Append(", ");
                    body.AppendLine();
                }

                body.Append($"{indent}{field.ParserMethodName}(data, \"{field.Name}\")");
            }

            body.AppendLine();
            body.Append(");");

            return CodeBlockBuilder.FromStringBuilder(body);
        }

        private bool IsNullable(ResultParserMethodDescriptor methodDescriptor)
        {
            if (methodDescriptor.IsNullable)
            {
                if (methodDescriptor.IsReferenceType)
                {
                    return NullableRefTypes;
                }
                return true;
            }
            return false;
        }
    }
}
