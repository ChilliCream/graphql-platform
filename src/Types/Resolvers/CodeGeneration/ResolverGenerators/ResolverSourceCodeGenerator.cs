using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal abstract class ResolverSourceCodeGenerator<T>
        : ResolverSourceCodeGeneratorBase<T>
        where T : IFieldResolverDescriptor
    {
        protected override IReadOnlyCollection<ArgumentSourceCodeGenerator> ArgumentGenerators =>
            ArgumentGeneratorCollections.ResolverArguments;

        protected override void GenerateDelegateHeader(
            string delegateName, T descriptor, StringBuilder source)
        {
            source.AppendLine($"/* {descriptor.Field.TypeName}.{descriptor.Field.FieldName} */");
            source.Append($"public static {nameof(FieldResolverDelegate)}");
            source.Append(" ");
            source.Append(delegateName);
            source.Append(" ");
            source.Append(" = ");
            source.Append("(ctx, ct) => {");
            source.AppendLine();
        }

        protected override void GenerateDelegateFooter(
            string delegateName, T descriptor, StringBuilder source)
        {
            source.AppendLine();
            source.Append("};");
        }

        protected override IEnumerable<ArgumentDescriptor> GetArguments(
            T descriptor)
        {
            return descriptor.Arguments;
        }
    }
}
