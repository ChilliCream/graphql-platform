﻿using System.Text;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class AsyncOnInvokeMethodGenerator
        : MiddlewareSourceCodeGenerator<DirectiveMiddlewareDescriptor>
    {
        protected override bool IsAsync => true;

        protected override void GenerateResolverInvocation(
            DirectiveMiddlewareDescriptor descriptor,
            StringBuilder source)
        {
            HandleExceptions(source, s =>
            {
                if (descriptor.HasResult)
                {
                    s.Append("ctx.Result = ");
                }
                s.Append($"await resolver.{descriptor.Method.Name}(");
                GenerateArguments(descriptor, s);
                s.AppendLine(");");
            });

            source.AppendLine("await next.Invoke(ctx);");
        }

        protected override bool CanHandle(
            DirectiveMiddlewareDescriptor descriptor)
        {
            return descriptor.IsAsync;
        }
    }
}
