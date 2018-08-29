using System;
using System.Text;
using HotChocolate.Internal;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal abstract class SourceCodeGenerator
    {
        public string Generate(
            string resolverName,
            IFieldResolverDescriptor resolverDescriptor)
        {
            var source = new StringBuilder();
            source.AppendLine($"/* {resolverDescriptor.Field.TypeName}.{resolverDescriptor.Field.FieldName} */");
            source.Append($"public static {nameof(FieldResolverDelegate)}");
            source.Append(" ");
            source.Append(resolverName);
            source.Append(" ");
            source.Append(" = ");
            source.Append("(ctx, ct) => {");
            source.AppendLine();

            foreach (ArgumentDescriptor argumentDescriptor in
                resolverDescriptor.Arguments)
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
            ArgumentDescriptor argumentDescriptor,
            StringBuilder source)
        {
            source.Append($"var {argumentDescriptor.VariableName} = ");
            switch (argumentDescriptor.Kind)
            {
                case ArgumentKind.Argument:
                    source.Append($"ctx.{nameof(IResolverContext.Argument)}<{GetTypeName(argumentDescriptor.Type)}>(\"{argumentDescriptor.Name}\")");
                    break;
                case ArgumentKind.Field:
                    source.Append($"ctx.{nameof(IResolverContext.Field)}");
                    break;
                case ArgumentKind.FieldSelection:
                    source.Append($"ctx.{nameof(IResolverContext.FieldSelection)}");
                    break;
                case ArgumentKind.ObjectType:
                    source.Append($"ctx.{nameof(IResolverContext.ObjectType)}");
                    break;
                case ArgumentKind.OperationDefinition:
                    source.Append($"ctx.{nameof(IResolverContext.Operation)}");
                    break;
                case ArgumentKind.QueryDocument:
                    source.Append($"ctx.{nameof(IResolverContext.QueryDocument)}");
                    break;
                case ArgumentKind.Schema:
                    source.Append($"ctx.{nameof(IResolverContext.Schema)}");
                    break;
                case ArgumentKind.Service:
                    source.Append($"ctx.{nameof(IResolverContext.Service)}<{GetTypeName(argumentDescriptor.Type)}>()");
                    break;
                case ArgumentKind.Source:
                    source.Append($"ctx.{nameof(IResolverContext.Parent)}<{GetTypeName(argumentDescriptor.Type)}>()");
                    break;
                case ArgumentKind.Context:
                    source.Append($"ctx");
                    break;
                case ArgumentKind.CancellationToken:
                    source.Append($"ct");
                    break;
                case ArgumentKind.DataLoader:
                    source.Append($"ctx.{nameof(IResolverContext.DataLoader)}<{GetTypeName(argumentDescriptor.Type)}>()");
                    break;
                case ArgumentKind.CustomContext:
                    source.Append($"ctx.{nameof(IResolverContext.CustomContext)}<{GetTypeName(argumentDescriptor.Type)}>()");
                    break;
                default:
                    throw new NotSupportedException();
            }
            source.Append(";");
        }

        protected abstract void GenerateResolverInvocation(
            IFieldResolverDescriptor resolverDescriptor,
            StringBuilder source);

        public abstract bool CanGenerate(
            IFieldResolverDescriptor resolverDescriptor);

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
            source.AppendLine($"catch(HotChocolate.Execution.QueryException ex)");
            source.AppendLine("{");
            source.AppendLine($"return ex.Errors;");
            source.AppendLine("}");
        }
    }

    internal abstract class SourceCodeGenerator<T>
        : SourceCodeGenerator
        where T : IFieldResolverDescriptor
    {
        protected sealed override void GenerateResolverInvocation(
            IFieldResolverDescriptor resolverDescriptor,
            StringBuilder source)
        {
            GenerateResolverInvocation((T)resolverDescriptor, source);
        }

        protected abstract void GenerateResolverInvocation(
            T resolverDescriptor,
            StringBuilder source);
    }
}
