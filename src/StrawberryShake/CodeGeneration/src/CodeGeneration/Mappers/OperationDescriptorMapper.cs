using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public static class OperationDescriptorMapper
    {
        public static void Map(
            ClientModel model,
            IMapperContext context)
        {
            foreach (OperationModel modelOperation in model.Operations)
            {
                var arguments = modelOperation.Arguments.Select(
                        arg =>
                        {
                            if (arg.Type.IsEnumType())
                            {
                                var enumType =
                                    context.EnumTypes.Single(type =>
                                        type.Name == arg.Type.TypeName());

                                return new PropertyDescriptor(
                                    arg.Name,
                                    Unwrap(arg.Type,
                                        _ => new NamedTypeDescriptor(
                                            enumType.Name,
                                            enumType.Namespace,
                                            false,
                                            graphQLTypeName: enumType.Name,
                                            isEnum: true)));
                            }

                            return new PropertyDescriptor(
                                arg.Name,
                                Unwrap(arg.Type,
                                    _ => context.Types.First(
                                        type => type.GraphQLTypeName == arg.Type.TypeName()
                                    )));
                        })
                    .ToList();

                switch (modelOperation.OperationType)
                {
                    case OperationType.Query:
                        context.Register(
                            modelOperation.Name,
                            new QueryOperationDescriptor(
                                modelOperation.Name,
                                context.Types.Single(
                                    t => t.Name.Equals(modelOperation.ResultType.Name)),
                                context.Namespace,
                                arguments,
                                modelOperation.Document.ToString()));
                        break;
                    case OperationType.Mutation:
                        context.Register(
                            modelOperation.Name,
                            new MutationOperationDescriptor(
                                modelOperation.Name,
                                context.Types.Single(
                                    t => t.Name.Equals(modelOperation.ResultType.Name)),
                                context.Namespace,
                                arguments,
                                modelOperation.Document.ToString()));
                        break;
                    case OperationType.Subscription:
                        context.Register(
                            modelOperation.Name,
                            new SubscriptionOperationDescriptor(
                                modelOperation.Name,
                                context.Types.Single(
                                    t => t.Name.Equals(modelOperation.ResultType.Name)),
                                context.Namespace,
                                arguments,
                                modelOperation.Document.ToString()));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static ITypeDescriptor Unwrap(
            IType type,
            Func<INamedType, NamedTypeDescriptor> mapNamedType)
        {
            switch (type)
            {
                case NonNullType descriptor:
                    return new NonNullTypeDescriptor(Unwrap(descriptor.InnerType(), mapNamedType));
                case ListType descriptor:
                    return new ListTypeDescriptor(Unwrap(descriptor.InnerType(), mapNamedType));
                case INamedType descriptor:
                    return mapNamedType(descriptor);
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
