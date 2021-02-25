using System;
using System.Text;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class OperationDocumentGenerator : ClassBaseGenerator<OperationDescriptor>
    {
        protected override void Generate(
            CodeWriter writer,
            OperationDescriptor descriptor,
            out string fileName)
        {
            var (classBuilder, constructorBuilder) = CreateClassBuilder();

            fileName = CreateDocumentTypeName(descriptor.Name);
            classBuilder
                .AddImplements(TypeNames.IDocument)
                .SetName(fileName);
            constructorBuilder.SetAccessModifier(AccessModifier.Private);

            string operationKind = descriptor switch
            {
                MutationOperationDescriptor => "Mutation",
                QueryOperationDescriptor => "Query",
                SubscriptionOperationDescriptor => "Subscription",
                _ => throw new ArgumentOutOfRangeException(nameof(descriptor))
            };

            classBuilder.AddProperty(
                PropertyBuilder.New()
                    .SetStatic()
                    .SetType(fileName)
                    .SetName("Instance")
                    .SetValue($"new {fileName}()"));

            classBuilder.AddProperty(
                "Kind",
                x => x.SetType(TypeNames.OperationKind)
                    .AsLambda($"{TypeNames.OperationKind}.{operationKind}"));

            classBuilder.AddProperty(
                PropertyBuilder.New()
                    .SetType(TypeNames.IReadOnlySpan.WithGeneric(TypeNames.Byte))
                    .SetName("Body")
                    .AsLambda(GetByteArray(descriptor.BodyString)));

            classBuilder.AddMethod(
                MethodBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .SetReturnType("override string")
                    .SetName("ToString")
                    .AddCode(MethodCallBuilder.New()
                        .SetMethodName($"return {TypeNames.EncodingUtf8}.GetString")
                        .AddArgument("Body")));

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.Namespace)
                .AddType(classBuilder)
                .Build(writer);
        }

        private static string GetByteArray(string value)
        {
            var builder = new StringBuilder();
            var bytes = Encoding.UTF8.GetBytes(value);
            builder.Append("new byte[]{ ");

            for (var i = 0; i < bytes.Length; i++)
            {
                builder.Append("0x");
                builder.Append(bytes[i].ToString("x2"));
                if (i < bytes.Length - 1)
                {
                    builder.Append(", ");
                }
            }

            builder.Append(" }");

            return builder.ToString();
        }
    }
}
