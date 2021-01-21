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

            classBuilder.SetName(DocumentTypeNameFromOperationName(descriptor.Name));
            constructorBuilder.SetAccessModifier(AccessModifier.Private);

            classBuilder.AddField(
                FieldBuilder.New()
                    .SetStatic()
                    .SetConst()
                    .SetType("string")
                    .SetName("_bodyString")
                    .SetValue($"@\"{descriptor.BodyString}\"", true));

            classBuilder.AddField(
                FieldBuilder.New()
                    .SetStatic()
                    .SetReadOnly()
                    .SetType("byte[]")
                    .SetName("_body")
                    .SetValue("Encoding.UTF8.GetBytes(_bodyString)"));

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
                    .SetType("GetHeroQueryDocument")
                    .SetName("Instance")
                    .SetValue($"new()"));

            classBuilder.AddProperty(
                PropertyBuilder.New()
                    .SetType("OperationKind")
                    .SetName("Kind").AsLambda($"OperationKind.{operationKind}"));

            classBuilder.AddProperty(
                PropertyBuilder.New()
                    .SetType("ReadOnlySpan<byte>")
                    .SetName("Body").AsLambda("_body"));

            classBuilder.AddMethod(
                MethodBuilder.New()
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
