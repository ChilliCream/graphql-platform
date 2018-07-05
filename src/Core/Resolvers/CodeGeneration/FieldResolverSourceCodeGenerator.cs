using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal class FieldResolverSourceCodeGenerator
    {
        internal const string Namespace = "HotChocolate.Resolvers.CodeGeneration";
        internal const string ClassName = "___CompiledResolvers";
        internal const string FullClassName = Namespace + "." + ClassName;

        private static readonly SourceCodeGenerator[] _generators =
        {
            new AsyncResolverMethodGenerator(),
            new SyncResolverMethodGenerator(),
            new ResolverPropertyGenerator(),

            new AsyncSourceMethodGenerator(),
            new SyncSourceMethodGenerator(),
            new SourcePropertyGenerator()
        };

        public string Generate(IEnumerable<FieldResolverDescriptor> fieldResolverDescriptors)
        {
            return GenerateClass(fieldResolverDescriptors);
        }

        private string GenerateClass(
            IEnumerable<FieldResolverDescriptor> fieldResolverDescriptors)
        {
            var source = new StringBuilder();

            source.AppendLine("using System;");
            source.AppendLine("using System.Collections.Generic;");
            source.AppendLine("using System.Diagnostics;");
            source.AppendLine("using System.IO;");
            source.AppendLine("using System.Linq;");
            source.AppendLine("using System.Threading;");
            source.AppendLine("using System.Threading.Tasks;");
            source.AppendLine("using HotChocolate;");
            source.AppendLine("using HotChocolate.Resolvers;");

            source.AppendLine($"namespace {Namespace}");
            source.AppendLine("{");
            source.AppendLine($"public static class {ClassName}");
            source.AppendLine("{");

            GenerateResolvers(fieldResolverDescriptors, source);

            source.AppendLine("}");
            source.AppendLine("}");
            return source.ToString();
        }

        private void GenerateResolvers(
            IEnumerable<FieldResolverDescriptor> fieldResolverDescriptors,
            StringBuilder source)
        {
            var i = 0;
            foreach (FieldResolverDescriptor resolverDescriptor in
                fieldResolverDescriptors)
            {
                string resolverName = GetResolverName(i++);
                SourceCodeGenerator generator = _generators.First(
                    t => t.CanGenerate(resolverDescriptor));
                source.AppendLine(generator.Generate(
                    resolverName, resolverDescriptor));
            }
        }

        public string GetResolverName(int index)
        {
            return "_" + index;
        }
    }
}
