using System.Text;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.Operations;
using StrawberryShake.CodeGeneration.Properties;
using StrawberryShake.Tools.Configuration;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public class OperationDocumentGenerator : ClassBaseGenerator<OperationDescriptor>
{
    protected override void Generate(OperationDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings,
        CodeWriter writer,
        out string fileName,
        out string? path,
        out string ns)
    {
        var documentName = CreateDocumentTypeName(descriptor.RuntimeType.Name);
        fileName = documentName;
        path = null;
        ns = descriptor.RuntimeType.NamespaceWithoutGlobal;

        var operationKind = descriptor switch
        {
            MutationOperationDescriptor => "Mutation",
            QueryOperationDescriptor => "Query",
            SubscriptionOperationDescriptor => "Subscription",
            _ => throw new ArgumentOutOfRangeException(nameof(descriptor)),
        };

        var classBuilder = ClassBuilder
            .New()
            .SetAccessModifier(settings.AccessModifier)
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

        if (descriptor.Strategy == RequestStrategy.PersistedOperation)
        {
            classBuilder
                .AddProperty("Body")
                .SetType(TypeNames.IReadOnlySpan.WithGeneric(TypeNames.Byte))
                .AsLambda($"new {TypeNames.Byte}[0]");
        }
        else
        {
            classBuilder
                .AddProperty("Body")
                .SetType(TypeNames.IReadOnlySpan.WithGeneric(TypeNames.Byte))
                .AsLambda(GetByteArray(descriptor.Body));
        }

        classBuilder
            .AddProperty("Hash")
            .SetType(TypeNames.DocumentHash)
            .SetValue(
                $@"new {TypeNames.DocumentHash}(" +
                $@"""{descriptor.HashAlgorithm}"", " +
                $@"""{descriptor.HashValue}"")");

        classBuilder
            .AddMethod("ToString")
            .SetPublic()
            .SetOverride()
            .SetReturnType(TypeNames.String)
            .AddCode("#if NETCOREAPP3_1_OR_GREATER")
            .AddCode(MethodCallBuilder
                .New()
                .SetReturn()
                .SetMethodName(TypeNames.EncodingUtf8, nameof(Encoding.UTF8.GetString))
                .AddArgument("Body"))
            .AddCode("#else")
            .AddCode(MethodCallBuilder
                .New()
                .SetReturn()
                .SetMethodName(TypeNames.EncodingUtf8, nameof(Encoding.UTF8.GetString))
                .AddArgument("Body.ToArray()"))
            .AddCode("#endif");

        classBuilder.Build(writer);
    }

    private static string GetByteArray(byte[] bytes)
    {
        var builder = new StringBuilder();
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
