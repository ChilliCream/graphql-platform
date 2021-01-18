using System;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class OperationDocumentGenerator: ClassBaseGenerator<OperationDescriptor>
    {
        protected override Task WriteAsync(
            CodeWriter writer, 
            OperationDescriptor operationDescriptor)
        {
            AssertNonNull(
                writer,
                operationDescriptor);

            ClassBuilder.SetName(DocumentTypeNameFromOperationName(operationDescriptor.Name));
            ConstructorBuilder.SetAccessModifier(AccessModifier.Private);

            ClassBuilder.AddField(
                FieldBuilder.New()
                    .SetStatic()
                    .SetConst()
                    .SetType("string")
                    .SetName("_bodyString")
                    .SetValue($"@\"{operationDescriptor.BodyString}\"", true));

            ClassBuilder.AddField(
                FieldBuilder.New()
                    .SetStatic()
                    .SetReadOnly()
                    .SetType("byte[]")
                    .SetName("_body")
                    .SetValue("Encoding.UTF8.GetBytes(_bodyString)"));

            string operationKind;
            switch (operationDescriptor)
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
                    throw new ArgumentOutOfRangeException(nameof(operationDescriptor));
            }

            ClassBuilder.AddProperty(
                PropertyBuilder.New()
                    .SetType("GetHeroQueryDocument")
                    .SetName("Instance")
                    .SetValue($"new()"));

            ClassBuilder.AddProperty(
                PropertyBuilder.New()
                    .SetType("OperationKind")
                    .SetName("Kind").AsLambda($"OperationKind.{operationKind}"));

            ClassBuilder.AddProperty(
                PropertyBuilder.New()
                    .SetType("ReadOnlySpan<byte>")
                    .SetName("Body").AsLambda("_body"));

            ClassBuilder.AddMethod(
                MethodBuilder.New()
                    .SetReturnType("override string")
                    .SetName("ToString")
                    .AddCode("return _bodyString;"));

            return CodeFileBuilder.New()
                .SetNamespace(operationDescriptor.Namespace)
                .AddType(ClassBuilder)
                .BuildAsync(writer);
        }
    }
}
