using System;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class OperationDocumentGenerator: ClassBaseGenerator<OperationDescriptor>
    {
        protected override void Generate(CodeWriter writer, OperationDescriptor descriptor)
        {
            var (classBuilder, constructorBuilder) = CreateClassBuilder();

            var documentClassName = DocumentTypeNameFromOperationName(descriptor.Name);
            classBuilder
                .AddImplements(TypeNames.IDocument)
                .SetName(documentClassName);
            constructorBuilder.SetAccessModifier(AccessModifier.Private);

            classBuilder.AddField(
                FieldBuilder.New()
                    .SetStatic()
                    .SetConst()
                    .SetType(TypeNames.String)
                    .SetName("_bodyString")
                    .SetValue($"@\"{descriptor.BodyString}\"", true));

            classBuilder.AddField(
                FieldBuilder.New()
                    .SetStatic()
                    .SetReadOnly()
                    .SetType("byte[]")
                    .SetName("_body")
                    .SetValue($"{TypeNames.EncodingUtf8}.GetBytes(_bodyString)"));

            string operationKind;
            switch (descriptor)
            {
                case MutationOperationDescriptor mutationOperationDescriptor:
                    operationKind = "Mutation";
                    break;
                case QueryOperationDescriptor queryOperationDescriptor:
                    operationKind = "Query";
                    break;
                case SubscriptionOperationDescriptor subscriptionOperationDescriptor:
                    operationKind = "Subscription";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(descriptor));
            }

            classBuilder.AddProperty(
                PropertyBuilder.New()
                    .SetStatic()
                    .SetType(documentClassName)
                    .SetName("Instance")
                    .SetValue($"new {documentClassName}()"));

            classBuilder.AddProperty(
                PropertyBuilder.New()
                    .SetType(TypeNames.OperationKind)
                    .SetName("Kind").AsLambda($"{TypeNames.OperationKind}.{operationKind}"));

            classBuilder.AddProperty(
                PropertyBuilder.New()
                    .SetType($"{TypeNames.IReadOnlySpan}<byte>")
                    .SetName("Body").AsLambda("_body"));

            classBuilder.AddMethod(
                MethodBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .SetReturnType("override string")
                    .SetName("ToString")
                    .AddCode("return _bodyString;"));

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.Namespace)
                .AddType(classBuilder)
                .Build(writer);
        }
    }
}
