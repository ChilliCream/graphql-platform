using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Internal;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal abstract class SourceCodeGenerator
    {
        public string Generate(
            string resolverName,
            FieldResolverDescriptor resolverDescriptor)
        {
            StringBuilder source = new StringBuilder();
            source.Append($"public static {nameof(FieldResolverDelegate)}");
            source.Append(" ");
            source.Append(resolverName);
            source.Append(" ");
            source.Append(" = ");
            source.Append("(ctx, ct) => {");
            source.AppendLine();

            foreach (FieldResolverArgumentDescriptor argumentDescriptor in resolverDescriptor.ArgumentDescriptors)
            {
                GenerateArgumentInvocation(argumentDescriptor, source);
                source.AppendLine();
            }

            GenerateResolverInvocation(resolverDescriptor, source);

            source.AppendLine();
            source.Append("};");
            return source.ToString();
        }

        private void GenerateArgumentInvocation(
            FieldResolverArgumentDescriptor argumentDescriptor,
            StringBuilder source)
        {
            source.Append($"var {argumentDescriptor.Name} = ");
            switch (argumentDescriptor.Kind)
            {
                case FieldResolverArgumentKind.Argument:
                    source.Append($"ctx.{nameof(IResolverContext.Argument)}<{GetTypeName(argumentDescriptor.Type)}>(\"{argumentDescriptor.Name}\")");
                    break;
                case FieldResolverArgumentKind.Field:
                    source.Append($"ctx.{nameof(IResolverContext.Field)}");
                    break;
                case FieldResolverArgumentKind.FieldSelection:
                    source.Append($"ctx.{nameof(IResolverContext.FieldSelection)}");
                    break;
                case FieldResolverArgumentKind.ObjectType:
                    source.Append($"ctx.{nameof(IResolverContext.ObjectType)}");
                    break;
                case FieldResolverArgumentKind.OperationDefinition:
                    source.Append($"ctx.{nameof(IResolverContext.Operation)}");
                    break;
                case FieldResolverArgumentKind.QueryDocument:
                    source.Append($"ctx.{nameof(IResolverContext.QueryDocument)}");
                    break;
                case FieldResolverArgumentKind.Schema:
                    source.Append($"ctx.{nameof(IResolverContext.Schema)}");
                    break;
                case FieldResolverArgumentKind.Service:
                    source.Append($"ctx.{nameof(IResolverContext.Service)}<{GetTypeName(argumentDescriptor.Type)}>()");
                    break;
                case FieldResolverArgumentKind.Source:
                    source.Append($"ctx.{nameof(IResolverContext.Parent)}<{GetTypeName(argumentDescriptor.Type)}>()");
                    break;
                case FieldResolverArgumentKind.Context:
                    source.Append($"ctx");
                    break;
                case FieldResolverArgumentKind.CancellationToken:
                    source.Append($"ct");
                    break;
                default:
                    throw new NotSupportedException();
            }
            source.Append(";");
        }

        protected abstract void GenerateResolverInvocation(
            FieldResolverDescriptor resolverDescriptor,
            StringBuilder source);

        public abstract bool CanGenerate(
            FieldResolverDescriptor resolverDescriptor);

        protected string GetTypeName(Type type)
        {
            return type.GetTypeName();
        }        

        protected void HandleExceptions(StringBuilder source, Action<StringBuilder> code)
        {
            source.AppendLine("try");
            source.AppendLine("{");
            code(source);
            source.AppendLine();
            source.AppendLine("}");
            source.AppendLine($"catch({GetTypeName(typeof(QueryException))} ex)");
            source.AppendLine("{");
            source.AppendLine($"return ex.{nameof(QueryException.Errors)};");
            source.AppendLine("}");
            source.AppendLine($"catch({GetTypeName(typeof(Exception))})");
            source.AppendLine("{");
            source.AppendLine($"return new {GetTypeName(typeof(QueryError))}(\"Internal resolver error\");");
            source.Append("}");
        }
    }
}
