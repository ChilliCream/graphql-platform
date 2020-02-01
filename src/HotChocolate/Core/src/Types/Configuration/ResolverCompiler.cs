using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.Expressions;
using ExpressionCompiler = HotChocolate.Resolvers.Expressions.ResolverCompiler;

namespace HotChocolate.Configuration
{
    internal static class ResolverCompiler
    {
        public static void Compile(
            IDictionary<FieldReference, RegisteredResolver> resolvers)
        {
            if (resolvers == null)
            {
                throw new ArgumentNullException(nameof(resolvers));
            }

            foreach (var item in resolvers.ToArray())
            {
                RegisteredResolver registered = item.Value;
                if (registered.Field is FieldMember member)
                {
                    ResolverDescriptor descriptor =
                        registered.IsSourceResolver
                            ? new ResolverDescriptor(
                                registered.SourceType,
                                member)
                            : new ResolverDescriptor(
                                registered.ResolverType,
                                registered.SourceType,
                                member);
                    resolvers[item.Key] = registered.WithField(
                        ExpressionCompiler.Resolve.Compile(descriptor));
                }
            }
        }
    }

}
