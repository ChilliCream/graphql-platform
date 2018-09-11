using System;
using System.Text;
using HotChocolate.Internal;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal interface ISourceCodeGenerator
    {
        string Generate(string delegateName, IDelegateDescriptor descriptor);

        bool CanHandle(IDelegateDescriptor descriptor);
    }

    internal abstract class SourceCodeGenerator<TDescriptor>
        : ISourceCodeGenerator
        where TDescriptor : IDelegateDescriptor
    {
        public string Generate(
            string delegateName,
            IDelegateDescriptor descriptor)
        {
            if (descriptor is TDescriptor d)
            {
                return Generate(delegateName, descriptor);
            }

            throw new NotSupportedException("Descriptor not supported.");
        }

        public bool CanHandle(IDelegateDescriptor descriptor)
        {
            if (descriptor is TDescriptor d)
            {
                return CanHandle(d);
            }

            return false;
        }

        protected abstract string Generate(
            string delegateName, TDescriptor descriptor);

        protected virtual bool CanHandle(TDescriptor descriptor) => true;
    }



    internal abstract class ResolverSourceCodeGenerator<T>
        : SourceCodeGenerator<T>
        where T : IFieldResolverDescriptor
    {
        protected override string Generate(
            string delegateName,
            T descriptor)
        {
            var source = new StringBuilder();
            source.AppendLine($"/* {descriptor.Field.TypeName}.{descriptor.Field.FieldName} */");
            source.Append($"public static {nameof(FieldResolverDelegate)}");
            source.Append(" ");
            source.Append(delegateName);
            source.Append(" ");
            source.Append(" = ");
            source.Append("(ctx, ct) => {");
            source.AppendLine();

            foreach (ArgumentDescriptor argumentDescriptor in
                descriptor.Arguments)
            {
                GenerateArgumentInvocation(argumentDescriptor, source);
                source.AppendLine();
            }

            GenerateResolverInvocation(descriptor, source);

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
                default:
                    throw new NotSupportedException();
            }
            source.Append(";");
        }

        protected abstract void GenerateResolverInvocation(
            T resolverDescriptor,
            StringBuilder source);

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

    internal abstract class ArgumentSourceCodeGenerator
        : SourceCodeGenerator<ArgumentDescriptor>
    {
        protected abstract ArgumentKind Kind { get; }

        protected sealed override bool CanHandle(ArgumentDescriptor descriptor)
        {
            return descriptor.Kind == Kind;
        }
    }
}
