using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
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
                switch (modelOperation.Operation.Operation)
                {
                    case OperationType.Query:
                        context.Register(
                            modelOperation.Name,
                            new QueryOperationDescriptor(
                                modelOperation.Name,
                                context.Types.Single(
                                    t => t.Name.Equals(modelOperation.ResultType.Name)),
                                context.Namespace,
                                new List<PropertyDescriptor>(),
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
                                new List<PropertyDescriptor>(),
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
                                new List<PropertyDescriptor>(),
                                modelOperation.Document.ToString()));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
