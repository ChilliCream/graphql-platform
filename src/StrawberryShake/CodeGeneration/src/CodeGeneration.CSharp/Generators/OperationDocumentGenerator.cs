using System;
using System.Text;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Properties;
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
            var documentName = CreateDocumentTypeName(descriptor.RuntimeType.Name);
            fileName = documentName;

            string operationKind = descriptor switch
            {
                MutationOperationDescriptor => "Mutation",
                QueryOperationDescriptor => "Query",
                SubscriptionOperationDescriptor => "Subscription",
                _ => throw new ArgumentOutOfRangeException(nameof(descriptor))
            };

            ClassBuilder classBuilder = ClassBuilder
                .New()
                .SetName(fileName)
                .AddImplements(TypeNames.IDocument)
                .SetComment(
                    XmlCommentBuilder
                        .New()
                        .SetSummary(
                            string.Format(
                                CodeGenerationResources.OperationServiceDescriptor_Description,
                                descriptor.Name))
                        .AddCode(descriptor.BodyString));

            classBuilder
                .AddConstructor()
                .SetPrivate();

            classBuilder
                .AddProperty("Instance")
                .SetStatic()
                .SetType(documentName)
                .SetValue($"new {documentName}()");

            classBuilder
                .AddProperty("Kind")
                .SetType(TypeNames.OperationKind)
                .AsLambda($"{TypeNames.OperationKind}.{operationKind}");

            classBuilder
                .AddProperty("Body")
                .SetType(TypeNames.IReadOnlySpan.WithGeneric(TypeNames.Byte))
                .AsLambda(GetByteArray(descriptor.BodyString));

            classBuilder
                .AddMethod("ToString")
                .SetPublic()
                .SetOverride()
                .SetReturnType(TypeNames.String)
                .AddCode(MethodCallBuilder
                    .New()
                    .SetReturn()
                    .SetMethodName(TypeNames.EncodingUtf8, nameof(Encoding.UTF8.GetString))
                    .AddArgument("Body"));

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(classBuilder)
                .Build(writer);
        }

        private static string GetByteArray(string value)
        {
            var builder = new StringBuilder();
            var bytes = Encoding.UTF8.GetBytes(value);
            builder.Append($"new {TypeNames.Byte}[]{{ ");

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
