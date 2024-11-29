using System.Text;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Descriptors.Operations;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.Mappers;

public static class OperationDescriptorMapper
{
    public static void Map(ClientModel model, IMapperContext context)
    {
        foreach (var modelOperation in model.Operations)
        {
            var hasUpload = false;
            var arguments = modelOperation.Arguments.Select(
                    arg =>
                    {
                        var typeName = arg.Type.TypeName();

                        var namedTypeDescriptor =
                            context.Types.Single(type => type.Name.EqualsOrdinal(typeName));

                        hasUpload = hasUpload || namedTypeDescriptor.HasUpload();

                        return new PropertyDescriptor(
                            arg.Name,
                            arg.Variable.Variable.Name.Value,
                            Rewrite(arg.Type, namedTypeDescriptor),
                            null);
                    })
                .ToList();

            var resultType = context.GetRuntimeType(
                modelOperation.ResultType.Name,
                Descriptors.TypeDescriptors.TypeKind.Result);

            var bodyString = modelOperation.Document.ToString();
            var body = Encoding.UTF8.GetBytes(modelOperation.Document.ToString(false));
            var hash = context.HashProvider.ComputeHash(body);

            switch (modelOperation.OperationType)
            {
                case OperationType.Query:
                    context.Register(
                        modelOperation.Name,
                        new QueryOperationDescriptor(
                            modelOperation.Name,
                            context.Namespace,
                            context.Types.Single(t => t.RuntimeType.Equals(resultType)),
                            arguments,
                            body,
                            bodyString,
                            context.HashProvider.Name,
                            hash,
                            hasUpload,
                            context.RequestStrategy));
                    break;

                case OperationType.Mutation:
                    context.Register(
                        modelOperation.Name,
                        new MutationOperationDescriptor(
                            modelOperation.Name,
                            context.Namespace,
                            context.Types.Single(t => t.RuntimeType.Equals(resultType)),
                            arguments,
                            body,
                            bodyString,
                            context.HashProvider.Name,
                            hash,
                            hasUpload,
                            context.RequestStrategy));
                    break;

                case OperationType.Subscription:
                    context.Register(
                        modelOperation.Name,
                        new SubscriptionOperationDescriptor(
                            modelOperation.Name,
                            context.Namespace,
                            context.Types.Single(t => t.RuntimeType.Equals(resultType)),
                            arguments,
                            body,
                            bodyString,
                            context.HashProvider.Name,
                            hash,
                            context.RequestStrategy));
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private static ITypeDescriptor Rewrite(
        IType type,
        INamedTypeDescriptor namedTypeDescriptor)
        => type switch
        {
            NonNullType nnt => new NonNullTypeDescriptor(
                Rewrite(nnt.InnerType(), namedTypeDescriptor)),
            ListType lt => new ListTypeDescriptor(
                Rewrite(lt.InnerType(), namedTypeDescriptor)),
            INamedType => namedTypeDescriptor,
            _ => throw new InvalidOperationException(),
        };
}
