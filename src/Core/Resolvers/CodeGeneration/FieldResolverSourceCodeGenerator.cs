using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HotChocolate.Resolvers.CodeGeneration
{
    public class FieldResolverSourceCodeGenerator
    {
        private static readonly SourceCodeGenerator[] _generators =
            new SourceCodeGenerator[]
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
            StringBuilder source = new StringBuilder();

            source.AppendLine("using System;");
            source.AppendLine("using System.Collections.Generic;");
            source.AppendLine("using System.Diagnostics;");
            source.AppendLine("using System.IO;");
            source.AppendLine("using System.Linq;");
            source.AppendLine("using System.Threading;");
            source.AppendLine("using System.Threading.Tasks;");
            source.AppendLine("using HotChocolate;");
            source.AppendLine("using HotChocolate.Resolvers;");
            source.AppendLine("using HotChocolate.Resolvers.CodeGeneration;");

            source.AppendLine("namespace HotChocolate.Resolvers.CodeGeneration");
            source.AppendLine("{");
            source.AppendLine("public static class ___CompiledResolvers");
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
            int i = 0;
            foreach (FieldResolverDescriptor resolverDescriptor in
                fieldResolverDescriptors)
            {
                string resolverName = "_" + i++;
                SourceCodeGenerator generator = _generators.First(
                    t => t.CanGenerate(resolverDescriptor));
                source.AppendLine(generator.Generate(
                    resolverName, resolverDescriptor));
            }
        }




    }
}