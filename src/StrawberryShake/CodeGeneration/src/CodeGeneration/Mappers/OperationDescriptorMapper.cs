using System;
using System.Linq;
using System.Text;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Descriptors.Operations;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public static class OperationDescriptorMapper
    {
        public static void Map(ClientModel model, IMapperContext context)
        {
            foreach (OperationModel modelOperation in model.Operations)
            {
                var arguments = modelOperation.Arguments.Select(
                    arg =>
                    {
                        NameString typeName = arg.Type.TypeName();

                        INamedTypeDescriptor namedTypeDescriptor =
                            context.Types.Single(type => type.Name.Equals(typeName));

                        return new PropertyDescriptor(
                            arg.Name,
                            arg.Variable.Variable.Name.Value,
                            Rewrite(arg.Type, namedTypeDescriptor),
                            null);
                    })
                    .ToList();

                RuntimeTypeInfo resultType = context.GetRuntimeType(
                    modelOperation.ResultType.Name,
                    Descriptors.TypeDescriptors.TypeKind.ResultType);

                string bodyString = modelOperation.Document.ToString();
                byte[] body = Encoding.UTF8.GetBytes(modelOperation.Document.ToString(false));
                string hash = context.HashProvider.ComputeHash(body);

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
        {
            switch (type)
            {
                case NonNullType nnt:
                    return new NonNullTypeDescriptor(Rewrite(nnt.InnerType(), namedTypeDescriptor));

                case ListType lt:
                    return new ListTypeDescriptor(Rewrite(lt.InnerType(), namedTypeDescriptor));

                case INamedType:
                    return namedTypeDescriptor;

                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
