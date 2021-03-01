using System;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using static StrawberryShake.CodeGeneration.NamingConventions;

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
                            Rewrite(arg.Type, namedTypeDescriptor),
                            null);
                    })
                    .ToList();

                var resultTypeName = CreateResultRootTypeName(modelOperation.ResultType.Name);

                switch (modelOperation.OperationType)
                {
                    case OperationType.Query:
                        context.Register(
                            modelOperation.Name,
                            new QueryOperationDescriptor(
                                modelOperation.Name,
                                context.Types.Single(t => t.RuntimeType.Name.Equals(resultTypeName)),
                                context.Namespace,
                                arguments,
                                modelOperation.Document.ToString()));
                        break;

                    case OperationType.Mutation:
                        context.Register(
                            modelOperation.Name,
                            new MutationOperationDescriptor(
                                modelOperation.Name,
                                context.Types.Single(t => t.RuntimeType.Name.Equals(resultTypeName)),
                                context.Namespace,
                                arguments,
                                modelOperation.Document.ToString()));
                        break;

                    case OperationType.Subscription:
                        context.Register(
                            modelOperation.Name,
                            new SubscriptionOperationDescriptor(
                                modelOperation.Name,
                                context.Types.Single(t => t.RuntimeType.Name.Equals(resultTypeName)),
                                context.Namespace,
                                arguments,
                                modelOperation.Document.ToString()));
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
