using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultParserGenerator
        : CodeGenerator<ResultParserDescriptor>
    {
        protected override Task WriteAsync(
            CodeWriter writer,
            ResultParserDescriptor descriptor)
        {
            ClassBuilder classBuilder =
                ClassBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .SetName(descriptor.Name);

            AddConstructor(
                classBuilder,
                descriptor.ValueSerializers,
                CodeWriter.Indent);

            return CodeFileBuilder.New()
                .SetNamespace(descriptor.Namespace)
                .AddType(classBuilder)
                .BuildAsync(writer);
        }

        private static void AddConstructor(
            ClassBuilder classBuilder,
            IReadOnlyList<ValueSerializerDescriptor> serializers,
            string indent)
        {
            foreach (ValueSerializerDescriptor serializer in serializers)
            {
                classBuilder.AddField(
                    FieldBuilder.New()
                        .SetType("global::StrawberryShake.IValueSerializer")
                        .SetName(serializer.FieldName));
            }

            classBuilder.AddConstructor(
                ConstructorBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .AddParameter(ParameterBuilder.New()
                        .SetType("global::StrawberryShake.IValueSerializerCollection")
                        .SetName("serializerResolver"))
                    .AddCode(CreateConstructorBody(serializers, indent)));
        }

        private static CodeBlockBuilder CreateConstructorBody(
            IReadOnlyList<ValueSerializerDescriptor> serializers,
            string indent)
        {
            var body = new StringBuilder();

            body.AppendLine("public OnReviewResultParser(IValueSerializerCollection serializerResolver)");
            body.AppendLine("if (serializerResolver is null)");
            body.AppendLine("{");
            body.AppendLine($"{indent}throw new ArgumentNullException(nameof(serializerResolver));");
            body.AppendLine("}");
            body.AppendLine();
            body.AppendLine();
            return CodeBlockBuilder.FromStringBuilder(body);
        }


    }
}
